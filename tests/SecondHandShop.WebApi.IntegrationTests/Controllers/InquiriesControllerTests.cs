using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using SecondHandShop.Application.Contracts.Inquiries;
using SecondHandShop.Application.UseCases.Inquiries;
using SecondHandShop.WebApi.Contracts;
using SecondHandShop.WebApi.Controllers;
using SecondHandShop.WebApi.IntegrationTests.Infrastructure;

namespace SecondHandShop.WebApi.IntegrationTests.Controllers;

[Collection("WebApiIntegration")]
public class InquiriesControllerTests
{
    private readonly TestWebApplicationFactory _factory;

    public InquiriesControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetAppMocks();
    }

    [Fact]
    public async Task Create_ShouldReturn201_WhenRequestIsValid()
    {
        var productId = Guid.NewGuid();
        var inquiryId = Guid.NewGuid();

        _factory.InquiryServiceMock
            .Setup(x => x.CreateInquiryAsync(
                It.Is<CreateInquiryCommand>(command =>
                    command.ProductId == productId
                    && command.CustomerName == "Alice"
                    && command.Email == "alice@example.com"
                    && command.PhoneNumber == "021 123 4567"
                    && command.Message == "Is this still available?"
                    && command.TurnstileToken == "turnstile-ok"
                    && command.RequestIpAddress == "203.0.113.5"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(inquiryId);

        using var client = CreateClient(("CF-Connecting-IP", "203.0.113.5"));

        var response = await client.PostAsJsonAsync("/api/inquiries", new
        {
            productId,
            customerName = "Alice",
            email = "alice@example.com",
            phoneNumber = "021 123 4567",
            message = "Is this still available?",
            turnstileToken = "turnstile-ok"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().Be($"/api/inquiries/{inquiryId}");
        var body = await response.Content.ReadFromJsonAsync<CreateInquiryResponse>();
        body.Should().NotBeNull();
        body!.InquiryId.Should().Be(inquiryId);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenTurnstileTokenIsMissing()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/inquiries", new
        {
            productId = Guid.NewGuid(),
            customerName = "Alice",
            email = "alice@example.com",
            phoneNumber = "021 123 4567",
            message = "Is this still available?"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenEmailIsInvalid()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/inquiries", new
        {
            productId = Guid.NewGuid(),
            customerName = "Alice",
            email = "not-an-email",
            phoneNumber = "021 123 4567",
            message = "Is this still available?",
            turnstileToken = "turnstile-ok"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenPhoneNumberIsInvalid()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/inquiries", new
        {
            productId = Guid.NewGuid(),
            customerName = "Alice",
            email = "alice@example.com",
            phoneNumber = "abc123",
            message = "Is this still available?",
            turnstileToken = "turnstile-ok"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenMessageIsTooLong()
    {
        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/inquiries", new
        {
            productId = Guid.NewGuid(),
            customerName = "Alice",
            email = "alice@example.com",
            phoneNumber = "021 123 4567",
            message = new string('x', 3001),
            turnstileToken = "turnstile-ok"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenTurnstileValidationFails()
    {
        _factory.InquiryServiceMock
            .Setup(x => x.CreateInquiryAsync(It.IsAny<CreateInquiryCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InquiryTurnstileValidationException("Turnstile verification failed."));

        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/inquiries", new
        {
            productId = Guid.NewGuid(),
            customerName = "Alice",
            email = "alice@example.com",
            phoneNumber = "021 123 4567",
            message = "Is this still available?",
            turnstileToken = "bad-turnstile"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body.Should().NotBeNull();
        body!.Message.Should().Be("Turnstile verification failed.");
    }

    [Fact]
    public async Task Create_ShouldReturn429_WhenRateLimitIsExceeded()
    {
        _factory.InquiryServiceMock
            .Setup(x => x.CreateInquiryAsync(It.IsAny<CreateInquiryCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InquiryRateLimitExceededException("Your IP is temporarily blocked for 5 minutes."));

        using var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/inquiries", new
        {
            productId = Guid.NewGuid(),
            customerName = "Alice",
            email = "alice@example.com",
            phoneNumber = "021 123 4567",
            message = "Is this still available?",
            turnstileToken = "turnstile-ok"
        });

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body.Should().NotBeNull();
        body!.Message.Should().Be("Your IP is temporarily blocked for 5 minutes.");
    }

    [Fact]
    public async Task Create_ShouldUseXForwardedFor_WhenCloudflareHeaderIsMissing()
    {
        var inquiryId = Guid.NewGuid();
        _factory.InquiryServiceMock
            .Setup(x => x.CreateInquiryAsync(
                It.Is<CreateInquiryCommand>(command => command.RequestIpAddress == "198.51.100.7"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(inquiryId);

        using var client = CreateClient(("X-Forwarded-For", "198.51.100.7, 10.0.0.1"));

        var response = await client.PostAsJsonAsync("/api/inquiries", new
        {
            productId = Guid.NewGuid(),
            customerName = "Alice",
            email = "alice@example.com",
            phoneNumber = "021 123 4567",
            message = "Is this still available?",
            turnstileToken = "turnstile-ok"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private HttpClient CreateClient(params (string Name, string Value)[] headers)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        foreach (var (name, value) in headers)
        {
            client.DefaultRequestHeaders.Add(name, value);
        }

        return client;
    }
}
