using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using SecondHandShop.Application.Contracts.Catalog;
using SecondHandShop.Application.Contracts.Common;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;
using SecondHandShop.WebApi.Controllers;
using SecondHandShop.WebApi.Contracts;
using SecondHandShop.WebApi.IntegrationTests.Infrastructure;

namespace SecondHandShop.WebApi.IntegrationTests.Controllers;

[Collection("WebApiIntegration")]
public class ProductsControllerTests
{
    private readonly TestWebApplicationFactory _factory;
    private static readonly DateTime UtcNow = new(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc);

    public ProductsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetAppMocks();
    }

    [Fact]
    public async Task Search_ShouldReturnFallbackResults_WhenSafeSearchHasNoMatches()
    {
        _factory.ProductRepositoryMock
            .SetupSequence(x => x.ListPagedForPublicAsync(It.IsAny<ProductQueryParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ProductListItemDto>([], 1, 24, 0))
            .ReturnsAsync(new PagedResult<ProductListItemDto>(
            [
                new ProductListItemDto(Guid.NewGuid(), "Vintage bag", "vintage-bag", 250m, "products/key.webp", "Bags", "Available", "Good", UtcNow)
            ],
            1,
            24,
            1));

        _factory.ObjectStorageServiceMock
            .Setup(x => x.BuildDisplayUrl("products/key.webp"))
            .Returns("https://img.example.test/products/key.webp");

        using var client = CreateClient();

        var response = await client.GetAsync("/api/products/search?search=no-match");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<ProductListItemResponse>>();
        body.Should().NotBeNull();
        body!.IsFallback.Should().BeTrue();
        body.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task ListFeatured_ShouldClampLimitToMax()
    {
        _factory.ProductRepositoryMock
            .Setup(x => x.ListFeaturedForPublicAsync(24, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        using var client = CreateClient();

        var response = await client.GetAsync("/api/products/featured?limit=99");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.ProductRepositoryMock.Verify(x => x.ListFeaturedForPublicAsync(24, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_ShouldReturn404_WhenProductDoesNotExist()
    {
        var productId = Guid.NewGuid();
        _factory.ProductRepositoryMock
            .Setup(x => x.GetPublicByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        using var client = CreateClient();

        var response = await client.GetAsync($"/api/products/{productId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body.Should().NotBeNull();
        body!.Message.Should().Be("Product was not found.");
    }

    [Fact]
    public async Task GetBySlug_ShouldReturn404_WhenProductDoesNotExist()
    {
        _factory.ProductRepositoryMock
            .Setup(x => x.GetPublicBySlugAsync("missing-slug", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        using var client = CreateClient();

        var response = await client.GetAsync("/api/products/slug/MISSING-SLUG");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _factory.ProductRepositoryMock.Verify(x => x.GetPublicBySlugAsync("missing-slug", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_ShouldReturnProduct_WithImagesAndActiveCategoryName()
    {
        var category = Category.Create("Bags", "bags", null, 1, true, null, UtcNow);
        var product = Product.Create("Vintage bag", "vintage-bag", "Soft grain leather bag.", 250m, category.Id, null, UtcNow, ProductCondition.Good);
        var image = ProductImage.Create(product.Id, "products/key.webp", "Bag cover", 0, true, null, UtcNow);

        _factory.ProductRepositoryMock
            .Setup(x => x.GetPublicByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _factory.CategoryRepositoryMock
            .Setup(x => x.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        _factory.ProductImageRepositoryMock
            .Setup(x => x.ListByProductIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([image]);
        _factory.ObjectStorageServiceMock
            .Setup(x => x.BuildDisplayUrl("products/key.webp"))
            .Returns("https://img.example.test/products/key.webp");

        using var client = CreateClient();

        var response = await client.GetAsync($"/api/products/{product.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProductResponse>();
        body.Should().NotBeNull();
        body!.CategoryName.Should().Be("Bags");
        body.Images.Should().HaveCount(1);
        body.Images[0].DisplayUrl.Should().Be("https://img.example.test/products/key.webp");
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
    }
}
