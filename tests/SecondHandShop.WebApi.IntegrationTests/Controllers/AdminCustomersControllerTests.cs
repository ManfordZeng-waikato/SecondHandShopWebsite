using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using SecondHandShop.Application.Common.Exceptions;
using SecondHandShop.Application.Contracts.Common;
using SecondHandShop.Application.Contracts.Customers;
using SecondHandShop.Application.Contracts.Sales;
using SecondHandShop.Application.UseCases.Customers;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;
using SecondHandShop.WebApi.Controllers;
using SecondHandShop.WebApi.Contracts;
using SecondHandShop.WebApi.IntegrationTests.Infrastructure;

namespace SecondHandShop.WebApi.IntegrationTests.Controllers;

[Collection("WebApiIntegration")]
public class AdminCustomersControllerTests
{
    private readonly TestWebApplicationFactory _factory;

    public AdminCustomersControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetAppMocks();
    }

    [Fact]
    public async Task List_ShouldReturn200_WithPagedCustomers()
    {
        var customerId = Guid.NewGuid();
        _factory.CustomerRepositoryMock
            .Setup(x => x.ListPagedForAdminAsync(It.IsAny<AdminCustomerQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<CustomerListItemDto>(
            [
                new CustomerListItemDto(
                    customerId,
                    "Alice",
                    "alice@example.com",
                    "021 123 4567",
                    "New",
                    "Inquiry",
                    "Inquiry",
                    2,
                    new DateTime(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc),
                    1,
                    220m,
                    new DateTime(2026, 4, 15, 1, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 4, 10, 1, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 4, 16, 2, 0, 0, DateTimeKind.Utc))
            ],
            1,
            20,
            1));

        using var client = CreateAdminClient();

        var response = await client.GetAsync("/api/lord/customers?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<CustomerListItemDto>>();
        body.Should().NotBeNull();
        body!.Items.Should().HaveCount(1);
        body.Items[0].Id.Should().Be(customerId);
    }

    [Fact]
    public async Task GetDetail_ShouldReturn404_WhenCustomerDoesNotExist()
    {
        var customerId = Guid.NewGuid();
        _factory.CustomerRepositoryMock
            .Setup(x => x.GetDetailForAdminAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerDetailDto?)null);

        using var client = CreateAdminClient();

        var response = await client.GetAsync($"/api/lord/customers/{customerId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body.Should().NotBeNull();
        body!.Message.Should().Be($"Customer '{customerId}' was not found.");
    }

    [Fact]
    public async Task Create_ShouldReturn201_WhenRequestIsValid()
    {
        var customerId = Guid.NewGuid();
        _factory.AdminCustomerServiceMock
            .Setup(x => x.CreateCustomerAsync(
                It.Is<CreateCustomerRequest>(request =>
                    request.Name == "Alice"
                    && request.Email == "alice@example.com"
                    && request.PhoneNumber == "021 123 4567"
                    && request.Status == CustomerStatus.New
                    && request.Notes == "VIP"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(customerId);

        using var client = CreateAdminClient();

        var response = await client.PostAsJsonAsync("/api/lord/customers", new CreateCustomerApiRequest
        {
            Name = "Alice",
            Email = "alice@example.com",
            PhoneNumber = "021 123 4567",
            Status = "New",
            Notes = "VIP"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreateCustomerApiResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(customerId);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenStatusIsUnsupported()
    {
        using var client = CreateAdminClient();

        var response = await client.PostAsJsonAsync("/api/lord/customers", new CreateCustomerApiRequest
        {
            Name = "Alice",
            Email = "alice@example.com",
            PhoneNumber = "021 123 4567",
            Status = "DormantForever"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body.Should().NotBeNull();
        body!.Message.Should().Be("Unsupported customer status 'DormantForever'.");
    }

    [Fact]
    public async Task Create_ShouldReturn409_WhenCustomerConflictOccurs()
    {
        var existingCustomerId = Guid.NewGuid();
        _factory.AdminCustomerServiceMock
            .Setup(x => x.CreateCustomerAsync(It.IsAny<CreateCustomerRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CustomerConflictException(existingCustomerId, "email", "A customer with this email already exists."));

        using var client = CreateAdminClient();

        var response = await client.PostAsJsonAsync("/api/lord/customers", new CreateCustomerApiRequest
        {
            Name = "Alice",
            Email = "alice@example.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await response.Content.ReadFromJsonAsync<CustomerConflictApiResponse>();
        body.Should().NotBeNull();
        body!.ExistingCustomerId.Should().Be(existingCustomerId);
        body.ConflictField.Should().Be("email");
    }

    [Fact]
    public async Task Update_ShouldReturn204_WhenRequestIsValid()
    {
        var customerId = Guid.NewGuid();
        _factory.AdminCustomerServiceMock
            .Setup(x => x.UpdateCustomerAsync(
                customerId,
                It.Is<UpdateCustomerRequest>(request =>
                    request.Name == "Alice Updated"
                    && request.PhoneNumber == "021 765 4321"
                    && request.Status == CustomerStatus.Contacted
                    && request.Notes == "Follow up"),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var client = CreateAdminClient();

        var response = await client.PatchAsJsonAsync(
            $"/api/lord/customers/{customerId}",
            new UpdateCustomerApiRequest
            {
                Name = "Alice Updated",
                PhoneNumber = "021 765 4321",
                Status = "Contacted",
                Notes = "Follow up"
            });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ListInquiries_ShouldReturn404_WhenCustomerDoesNotExist()
    {
        var customerId = Guid.NewGuid();
        _factory.CustomerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        using var client = CreateAdminClient();

        var response = await client.GetAsync($"/api/lord/customers/{customerId}/inquiries");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body.Should().NotBeNull();
        body!.Message.Should().Be($"Customer '{customerId}' was not found.");
    }

    [Fact]
    public async Task ListSales_ShouldReturn404_WhenCustomerDoesNotExist()
    {
        var customerId = Guid.NewGuid();
        _factory.CustomerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        using var client = CreateAdminClient();

        var response = await client.GetAsync($"/api/lord/customers/{customerId}/sales");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body.Should().NotBeNull();
        body!.Message.Should().Be($"Customer '{customerId}' was not found.");
    }

    [Fact]
    public async Task ListSales_ShouldReturn200_WhenCustomerExists()
    {
        var customerId = Guid.NewGuid();
        var customer = Customer.Create("Alice", "alice@example.com", "021 123 4567", CustomerSource.Inquiry, new DateTime(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc));
        var sales = new[]
        {
            new CustomerSaleItemDto(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Vintage bag",
                "vintage-bag",
                220m,
                new DateTime(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc),
                "Cash",
                null)
        };

        _factory.CustomerRepositoryMock
            .Setup(x => x.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _factory.AdminSaleServiceMock
            .Setup(x => x.ListByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sales);

        using var client = CreateAdminClient();

        var response = await client.GetAsync($"/api/lord/customers/{customerId}/sales");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<CustomerSaleItemDto>>();
        body.Should().NotBeNull();
        body!.Should().HaveCount(1);
        body[0].ProductTitle.Should().Be("Vintage bag");
    }

    private HttpClient CreateAdminClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        client.DefaultRequestHeaders.Add(
            "Cookie",
            TestWebApplicationFactory.CreateCookieHeader(_factory.CreateAdminToken()));

        return client;
    }
}
