using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Moq;
using SecondHandShop.Application.Contracts.Admin;
using SecondHandShop.Application.Contracts.Catalog;
using SecondHandShop.Application.Contracts.Common;
using SecondHandShop.Application.UseCases.Admin.Login;
using SecondHandShop.WebApi.IntegrationTests.Infrastructure;

namespace SecondHandShop.WebApi.IntegrationTests.Middleware;

[Collection("WebApiIntegration")]
public class RateLimitingTests
{
    private readonly TestWebApplicationFactory _factory;

    public RateLimitingTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetAppMocks();
    }

    [Fact]
    public async Task LoginEndpoint_ShouldReturn429_OnSixthAttemptWithinSameWindow()
    {
        _factory.MediatorMock
            .Setup(x => x.Send(It.IsAny<LoginAdminCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginAdminResponse(
                "jwt",
                new DateTimeOffset(new DateTime(2026, 4, 20, 0, 20, 0, DateTimeKind.Utc)),
                false));

        // Use a per-test IP so previous test runs can't exhaust this partition.
        var clientIp = UniqueIp();
        using var client = _factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Add("CF-Connecting-IP", clientIp);

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 6; i++)
        {
            var response = await client.PostAsJsonAsync(
                "/api/lord/auth/login",
                new LoginAdminRequest("lord", "password"));
            statuses.Add(response.StatusCode);
        }

        statuses.Take(5).Should().OnlyContain(s => s == HttpStatusCode.OK,
            "the first 5 requests within the 1-minute window must pass the rate limiter");
        statuses[5].Should().Be(HttpStatusCode.TooManyRequests,
            "the 6th login attempt from the same IP within the window must be rejected");
    }

    [Fact]
    public async Task LoginEndpoint_ShouldKeepSeparateLimits_PerIp()
    {
        _factory.MediatorMock
            .Setup(x => x.Send(It.IsAny<LoginAdminCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginAdminResponse(
                "jwt",
                new DateTimeOffset(new DateTime(2026, 4, 20, 0, 20, 0, DateTimeKind.Utc)),
                false));

        var ipA = UniqueIp();
        var ipB = UniqueIp();

        for (var i = 0; i < 5; i++)
        {
            using var clientA = CreateLoginClient(ipA);
            var response = await clientA.PostAsJsonAsync(
                "/api/lord/auth/login",
                new LoginAdminRequest("lord", "password"));
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // A 6th call from ipA would be blocked, but ipB must still be fresh.
        using var clientB = CreateLoginClient(ipB);
        var responseB = await clientB.PostAsJsonAsync(
            "/api/lord/auth/login",
            new LoginAdminRequest("lord", "password"));

        responseB.StatusCode.Should().Be(HttpStatusCode.OK,
            "rate limit partitions are keyed on client IP, so a different IP must not be blocked");
    }

    [Fact]
    public async Task SearchEndpoint_ShouldReturn429_AfterPermitsAreExhaustedForSameIp()
    {
        _factory.ProductRepositoryMock
            .Setup(x => x.ListPagedForPublicAsync(It.IsAny<ProductQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ProductListItemDto>([], 1, 24, 0));

        var clientIp = UniqueIp();
        using var client = _factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Add("CF-Connecting-IP", clientIp);

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 31; i++)
        {
            var response = await client.GetAsync("/api/products/search?page=1&pageSize=24");
            statuses.Add(response.StatusCode);
        }

        statuses.Take(30).Should().OnlyContain(s => s == HttpStatusCode.OK,
            "the SearchRateLimit permit is 30 per sliding minute-window");
        statuses[30].Should().Be(HttpStatusCode.TooManyRequests);
    }

    private HttpClient CreateLoginClient(string clientIp)
    {
        var client = _factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Add("CF-Connecting-IP", clientIp);
        return client;
    }

    private static string UniqueIp()
    {
        // Any stable per-test string works as the partition key — no real IP required.
        return $"203.0.113.{Random.Shared.Next(1, 255)}-{Guid.NewGuid():N}";
    }
}
