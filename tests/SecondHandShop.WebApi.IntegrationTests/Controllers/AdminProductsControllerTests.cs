using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using SecondHandShop.Application.Contracts.Catalog;
using SecondHandShop.Application.Contracts.Common;
using SecondHandShop.Application.UseCases.Catalog;
using SecondHandShop.Application.UseCases.Catalog.ProductCategories;
using SecondHandShop.Domain.Enums;
using SecondHandShop.WebApi.Controllers;
using SecondHandShop.WebApi.Contracts;
using SecondHandShop.WebApi.IntegrationTests.Infrastructure;

namespace SecondHandShop.WebApi.IntegrationTests.Controllers;

[Collection("WebApiIntegration")]
public class AdminProductsControllerTests
{
    private readonly TestWebApplicationFactory _factory;

    public AdminProductsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetAppMocks();
    }

    [Fact]
    public async Task List_ShouldReturn200_AndMapDisplayUrl()
    {
        var productId = Guid.NewGuid();
        _factory.ProductRepositoryMock
            .Setup(x => x.ListPagedForAdminAsync(It.IsAny<AdminProductQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<AdminProductListItemDto>(
            [
                new AdminProductListItemDto(
                    productId,
                    "Vintage bag",
                    "vintage-bag",
                    250m,
                    "Good",
                    "Available",
                    "Bags",
                    2,
                    "products/key.webp",
                    true,
                    1,
                    new DateTime(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc),
                    new DateTime(2026, 4, 16, 2, 0, 0, DateTimeKind.Utc))
            ],
            1,
            20,
            1));

        _factory.ObjectStorageServiceMock
            .Setup(x => x.BuildDisplayUrl("products/key.webp"))
            .Returns("https://img.example.test/products/key.webp");

        using var client = CreateAdminClient();

        var response = await client.GetAsync("/api/lord/products?page=1&pageSize=20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<AdminProductListItem>>();
        body.Should().NotBeNull();
        body!.Items.Should().HaveCount(1);
        body.Items[0].Id.Should().Be(productId);
        body.Items[0].PrimaryImageUrl.Should().Be("https://img.example.test/products/key.webp");
    }

    [Fact]
    public async Task Create_ShouldReturn201_WhenRequestIsValid()
    {
        var productId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        _factory.AdminCatalogServiceMock
            .Setup(x => x.CreateProductAsync(
                It.Is<CreateProductRequest>(request =>
                    request.Title == "Vintage bag"
                    && request.Slug == "vintage-bag"
                    && request.Description == "Soft grain leather bag."
                    && request.Price == 250m
                    && request.CategoryId == categoryId
                    && request.AdminUserId == _factory.ActiveAdminId
                    && request.Condition == ProductCondition.Good),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(productId);

        using var client = CreateAdminClient();

        var response = await client.PostAsJsonAsync("/api/lord/products", new
        {
            title = "Vintage bag",
            slug = "vintage-bag",
            description = "Soft grain leather bag.",
            price = 250m,
            categoryId,
            condition = "Good"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.ToString().Should().Be($"/api/lord/products/{productId}");
        var body = await response.Content.ReadFromJsonAsync<CreateProductResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().Be(productId);
    }

    [Fact]
    public async Task Create_ShouldReturn400_WhenConditionIsUnsupported()
    {
        using var client = CreateAdminClient();

        var response = await client.PostAsJsonAsync("/api/lord/products", new
        {
            title = "Vintage bag",
            slug = "vintage-bag",
            description = "Soft grain leather bag.",
            price = 250m,
            categoryId = Guid.NewGuid(),
            condition = "DamagedBeyondRepair"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body.Should().NotBeNull();
        body!.Message.Should().Be("Unsupported product condition 'DamagedBeyondRepair'.");
    }

    [Fact]
    public async Task UpdateStatus_ShouldReturn400_WhenStatusIsUnsupported()
    {
        using var client = CreateAdminClient();

        var response = await client.PutAsJsonAsync(
            $"/api/lord/products/{Guid.NewGuid()}/status",
            new
            {
                status = "Archived"
            });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body.Should().NotBeNull();
        body!.Message.Should().Be("Unsupported product status 'Archived'.");
    }

    [Fact]
    public async Task UpdateStatus_ShouldReturn204_WhenStatusIsValid()
    {
        var productId = Guid.NewGuid();
        _factory.AdminCatalogServiceMock
            .Setup(x => x.UpdateProductStatusAsync(
                productId,
                ProductStatus.OffShelf,
                _factory.ActiveAdminId,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var client = CreateAdminClient();

        var response = await client.PutAsJsonAsync(
            $"/api/lord/products/{productId}/status",
            new
            {
                status = "OffShelf"
            });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateFeatured_ShouldReturn204_WhenRequestIsValid()
    {
        var productId = Guid.NewGuid();
        _factory.AdminCatalogServiceMock
            .Setup(x => x.UpdateProductFeaturedAsync(
                productId,
                true,
                3,
                _factory.ActiveAdminId,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var client = CreateAdminClient();

        var response = await client.PutAsJsonAsync(
            $"/api/lord/products/{productId}/featured",
            new
            {
                isFeatured = true,
                featuredSortOrder = 3
            });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetCategorySelection_ShouldReturn200_WhenMediatorReturnsSelection()
    {
        var productId = Guid.NewGuid();
        var mainCategoryId = Guid.NewGuid();
        var selectedIds = new[] { mainCategoryId, Guid.NewGuid() };

        _factory.MediatorMock
            .Setup(x => x.Send(
                It.Is<GetProductCategorySelectionQuery>(query => query.ProductId == productId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductCategorySelectionDto(productId, mainCategoryId, selectedIds));

        using var client = CreateAdminClient();

        var response = await client.GetAsync($"/api/lord/products/{productId}/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProductCategorySelectionResponse>();
        body.Should().NotBeNull();
        body!.ProductId.Should().Be(productId);
        body.MainCategoryId.Should().Be(mainCategoryId);
        body.SelectedCategoryIds.Should().BeEquivalentTo(selectedIds);
    }

    [Fact]
    public async Task UpdateCategories_ShouldReturn200_WhenMediatorUpdatesSelection()
    {
        var productId = Guid.NewGuid();
        var mainCategoryId = Guid.NewGuid();
        var selectedIds = new[] { mainCategoryId, Guid.NewGuid() };

        _factory.MediatorMock
            .Setup(x => x.Send(
                It.Is<UpdateProductCategoriesCommand>(command =>
                    command.ProductId == productId
                    && command.MainCategoryId == mainCategoryId
                    && command.AdminUserId == _factory.ActiveAdminId
                    && command.SelectedCategoryIds != null
                    && command.SelectedCategoryIds.SequenceEqual(selectedIds)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductCategorySelectionDto(productId, mainCategoryId, selectedIds));

        using var client = CreateAdminClient();

        var response = await client.PutAsJsonAsync(
            $"/api/lord/products/{productId}/categories",
            new
            {
                mainCategoryId,
                selectedCategoryIds = selectedIds
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProductCategorySelectionResponse>();
        body.Should().NotBeNull();
        body!.SelectedCategoryIds.Should().BeEquivalentTo(selectedIds);
    }

    [Fact]
    public async Task CreateImageUploadUrl_ShouldReturn200_WhenRequestIsValid()
    {
        var productId = Guid.NewGuid();
        _factory.AdminCatalogServiceMock
            .Setup(x => x.CreateProductImageUploadUrlAsync(
                It.Is<CreateProductImageUploadUrlRequest>(request =>
                    request.ProductId == productId
                    && request.FileName == "bag.webp"
                    && request.ContentType == "image/webp"
                    && request.AdminUserId == _factory.ActiveAdminId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateProductImageUploadUrlResponse(
                "https://upload.example.test",
                $"products/{productId:N}/bag.webp",
                300));

        using var client = CreateAdminClient();

        var response = await client.PostAsJsonAsync(
            $"/api/lord/products/{productId}/images/presigned-url",
            new
            {
                fileName = "bag.webp",
                contentType = "image/webp"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<SecondHandShop.WebApi.Controllers.CreateImageUploadUrlResponse>();
        body.Should().NotBeNull();
        body!.PutUrl.Should().Be("https://upload.example.test");
        body.ObjectKey.Should().Contain(productId.ToString("N"));
    }

    [Fact]
    public async Task AddImage_ShouldReturn204_WhenRequestIsValid()
    {
        var productId = Guid.NewGuid();
        var objectKey = $"products/{productId:N}/bag.webp";

        _factory.AdminCatalogServiceMock
            .Setup(x => x.AddProductImageAsync(
                It.Is<AddProductImageRequest>(request =>
                    request.ProductId == productId
                    && request.ObjectKey == objectKey
                    && request.AltText == "Bag cover"
                    && request.SortOrder == 0
                    && request.IsPrimary
                    && request.AdminUserId == _factory.ActiveAdminId),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var client = CreateAdminClient();

        var response = await client.PostAsJsonAsync(
            $"/api/lord/products/{productId}/images",
            new
            {
                objectKey,
                altText = "Bag cover",
                sortOrder = 0,
                isPrimary = true
            });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteImage_ShouldReturn204_WhenRequestIsValid()
    {
        var productId = Guid.NewGuid();
        var imageId = Guid.NewGuid();

        _factory.AdminCatalogServiceMock
            .Setup(x => x.DeleteProductImageAsync(
                productId,
                imageId,
                _factory.ActiveAdminId,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var client = CreateAdminClient();

        var response = await client.DeleteAsync($"/api/lord/products/{productId}/images/{imageId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
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
