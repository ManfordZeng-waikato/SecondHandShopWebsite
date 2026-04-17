using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using SecondHandShop.Application.Contracts.Sales;
using SecondHandShop.Application.Common.Exceptions;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;
using SecondHandShop.WebApi.Contracts;
using SecondHandShop.WebApi.IntegrationTests.Infrastructure;

namespace SecondHandShop.WebApi.IntegrationTests.Controllers;

[Collection("WebApiIntegration")]
public class AdminProductSalesControllerTests
{
    private readonly TestWebApplicationFactory _factory;

    public AdminProductSalesControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetAppMocks();
    }

    [Fact]
    public async Task GetCurrentSale_ShouldReturn404_WhenNoActiveSaleExists()
    {
        var productId = Guid.NewGuid();
        _factory.AdminSaleServiceMock
            .Setup(x => x.GetCurrentSaleAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductSaleDto?)null);

        using var client = CreateAdminClient();

        var response = await client.GetAsync($"/api/lord/products/{productId}/sale");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error.Should().NotBeNull();
        error!.Message.Should().Be("No active sale record found for this product.");
    }

    [Fact]
    public async Task MarkSold_ShouldReturn201_WhenRequestIsValid()
    {
        var productId = Guid.NewGuid();
        var soldAtUtc = new DateTime(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc);
        var sale = new ProductSaleDto(
            Guid.NewGuid(),
            productId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            250m,
            220m,
            "Alice",
            "021 123 4567",
            "alice@example.com",
            soldAtUtc,
            "Cash",
            "Paid in full",
            "Completed",
            null,
            null,
            null,
            _factory.ActiveAdminId,
            soldAtUtc,
            soldAtUtc);

        _factory.AdminSaleServiceMock
            .Setup(x => x.MarkAsSoldAsync(
                It.Is<MarkProductSoldRequest>(request =>
                    request.ProductId == productId
                    && request.FinalSoldPrice == 220m
                    && request.BuyerName == "Alice"
                    && request.BuyerEmail == "alice@example.com"
                    && request.PaymentMethod == "Cash"
                    && request.AdminUserId == _factory.ActiveAdminId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(sale);

        using var client = CreateAdminClient();

        var response = await client.PostAsJsonAsync(
            $"/api/lord/products/{productId}/mark-sold",
            new
            {
                finalSoldPrice = 220m,
                soldAtUtc,
                customerId = sale.CustomerId,
                inquiryId = sale.InquiryId,
                buyerName = "Alice",
                buyerPhone = "021 123 4567",
                buyerEmail = "alice@example.com",
                paymentMethod = "Cash",
                notes = "Paid in full"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Be($"/api/lord/products/{productId}/sale");

        var body = await response.Content.ReadFromJsonAsync<ProductSaleDto>();
        body.Should().NotBeNull();
        body!.ProductId.Should().Be(productId);
        body.FinalSoldPrice.Should().Be(220m);
    }

    [Fact]
    public async Task RevertSale_ShouldReturn204_WhenReasonIsValid()
    {
        var productId = Guid.NewGuid();

        _factory.AdminSaleServiceMock
            .Setup(x => x.RevertSaleAsync(
                It.Is<RevertProductSaleRequest>(request =>
                    request.ProductId == productId
                    && request.Reason == SaleCancellationReason.AdminMistake
                    && request.CancellationNote == "Duplicate sale"
                    && request.AdminUserId == _factory.ActiveAdminId),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var client = CreateAdminClient();

        var response = await client.PostAsJsonAsync(
            $"/api/lord/products/{productId}/revert-sale",
            new
            {
                reason = "AdminMistake",
                cancellationNote = "Duplicate sale"
            });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RevertSale_ShouldReturn400_WhenReasonIsUnsupported()
    {
        var productId = Guid.NewGuid();
        using var client = CreateAdminClient();

        var response = await client.PostAsJsonAsync(
            $"/api/lord/products/{productId}/revert-sale",
            new
            {
                reason = "NotARealReason",
                cancellationNote = "Ignored"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error.Should().NotBeNull();
        error!.Message.Should().Be("Unsupported cancellation reason 'NotARealReason'.");
    }

    [Fact]
    public async Task GetHistory_ShouldReturnHistoryList()
    {
        var productId = Guid.NewGuid();
        var history = new[]
        {
            new ProductSaleDto(
                Guid.NewGuid(),
                productId,
                null,
                null,
                250m,
                220m,
                "Alice",
                null,
                null,
                new DateTime(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc),
                null,
                null,
                "Completed",
                null,
                null,
                null,
                _factory.ActiveAdminId,
                new DateTime(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc)),
            new ProductSaleDto(
                Guid.NewGuid(),
                productId,
                null,
                null,
                250m,
                210m,
                "Bob",
                null,
                null,
                new DateTime(2026, 4, 15, 1, 0, 0, DateTimeKind.Utc),
                null,
                null,
                "Cancelled",
                new DateTime(2026, 4, 16, 2, 0, 0, DateTimeKind.Utc),
                "AdminMistake",
                "Incorrect buyer",
                _factory.ActiveAdminId,
                new DateTime(2026, 4, 15, 1, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 4, 16, 2, 0, 0, DateTimeKind.Utc))
        };

        _factory.AdminSaleServiceMock
            .Setup(x => x.GetSaleHistoryAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        using var client = CreateAdminClient();

        var response = await client.GetAsync($"/api/lord/products/{productId}/sales");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<List<ProductSaleDto>>();
        body.Should().NotBeNull();
        body!.Should().HaveCount(2);
        body[0].BuyerName.Should().Be("Alice");
        body[1].Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task ListInquiries_ShouldReturn404_WhenProductDoesNotExist()
    {
        var productId = Guid.NewGuid();
        _factory.ProductRepositoryMock
            .Setup(x => x.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        using var client = CreateAdminClient();

        var response = await client.GetAsync($"/api/lord/products/{productId}/inquiries");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error.Should().NotBeNull();
        error!.Message.Should().Be($"Product '{productId}' was not found.");
    }

    [Fact]
    public async Task MarkSold_ShouldReturn404_WhenSaleServiceThrowsNotFound()
    {
        var productId = Guid.NewGuid();
        _factory.AdminSaleServiceMock
            .Setup(x => x.MarkAsSoldAsync(It.IsAny<MarkProductSoldRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException($"Product '{productId}' not found."));

        using var client = CreateAdminClient();

        var response = await client.PostAsJsonAsync(
            $"/api/lord/products/{productId}/mark-sold",
            new
            {
                finalSoldPrice = 220m,
                soldAtUtc = DateTime.UtcNow
            });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error.Should().NotBeNull();
        error!.Message.Should().Be($"Product '{productId}' not found.");
    }

    [Fact]
    public async Task RevertSale_ShouldReturn409_WhenSaleServiceThrowsConflict()
    {
        var productId = Guid.NewGuid();
        _factory.AdminSaleServiceMock
            .Setup(x => x.RevertSaleAsync(It.IsAny<RevertProductSaleRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException("Only sold products can be reverted to available."));

        using var client = CreateAdminClient();

        var response = await client.PostAsJsonAsync(
            $"/api/lord/products/{productId}/revert-sale",
            new
            {
                reason = "AdminMistake"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error.Should().NotBeNull();
        error!.Message.Should().Be("Only sold products can be reverted to available.");
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
