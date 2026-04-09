using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Abstractions.Storage;
using SecondHandShop.Application.Contracts.Catalog;
using SecondHandShop.Application.Contracts.Common;
using SecondHandShop.WebApi.Contracts;

namespace SecondHandShop.WebApi.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IProductImageRepository productImageRepository,
    IObjectStorageService objectStorageService) : ControllerBase
{
    private const int DefaultFeaturedLimit = 8;
    private const int MaxFeaturedLimit = 24;

    [HttpGet("search")]
    [EnableRateLimiting("SearchRateLimit")]
    public async Task<ActionResult<PagedResult<ProductListItemResponse>>> SearchAsync(
        [FromQuery] ProductQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await productRepository.ListPagedForPublicAsync(parameters, cancellationToken);

        var isFallback = false;
        if (result.TotalCount == 0 && parameters.SafeSearch is not null)
        {
            var fallbackParams = parameters with { Search = null, Page = 1 };
            result = await productRepository.ListPagedForPublicAsync(fallbackParams, cancellationToken);
            isFallback = true;
        }

        var items = result.Items
            .Select(ToProductListItemResponse)
            .ToList();

        var response = new PagedResult<ProductListItemResponse>(
            items, result.Page, result.PageSize, result.TotalCount, isFallback);

        return Ok(response);
    }

    [HttpGet("featured")]
    public async Task<ActionResult<IReadOnlyList<ProductListItemResponse>>> ListFeaturedAsync(
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var safeLimit = Math.Clamp(limit ?? DefaultFeaturedLimit, 1, MaxFeaturedLimit);
        var featuredProducts = await productRepository.ListFeaturedForPublicAsync(safeLimit, cancellationToken);
        return Ok(featuredProducts.Select(ToProductListItemResponse).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetPublicByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return NotFound(new ErrorResponse("Product was not found."));
        }

        var categoryName = await ResolveCategoryNameForPublicProductAsync(product.CategoryId, cancellationToken);
        var images = await productImageRepository.ListByProductIdAsync(product.Id, cancellationToken);
        return Ok(ToProductResponse(product, images, categoryName));
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<ProductResponse>> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var product = await productRepository.GetPublicBySlugAsync(normalizedSlug, cancellationToken);
        if (product is null)
        {
            return NotFound(new ErrorResponse("Product was not found."));
        }

        var categoryName = await ResolveCategoryNameForPublicProductAsync(product.CategoryId, cancellationToken);
        var images = await productImageRepository.ListByProductIdAsync(product.Id, cancellationToken);
        return Ok(ToProductResponse(product, images, categoryName));
    }

    /// <summary>
    /// Single category by id (PK lookup). Only exposes the name when the category is active, matching list/search behavior.
    /// </summary>
    private async Task<string?> ResolveCategoryNameForPublicProductAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        return category is { IsActive: true } ? category.Name : null;
    }

    private ProductResponse ToProductResponse(
        Domain.Entities.Product product,
        IReadOnlyList<Domain.Entities.ProductImage> images,
        string? categoryName)
    {
        return new ProductResponse(
            product.Id,
            product.Title,
            product.Slug,
            product.Description,
            product.Price,
            product.Condition?.ToString(),
            product.Status.ToString(),
            product.CategoryId,
            categoryName,
            images
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.CreatedAt)
                .Select(x => new ProductImageResponse(
                    x.Id,
                    x.CloudStorageKey,
                    objectStorageService.BuildDisplayUrl(x.CloudStorageKey),
                    x.AltText,
                    x.SortOrder,
                    x.IsPrimary))
                .ToList(),
            product.CreatedAt,
            product.UpdatedAt);
    }

    private ProductListItemResponse ToProductListItemResponse(ProductListItemDto dto)
    {
        return new ProductListItemResponse(
            dto.Id,
            dto.Title,
            dto.Slug,
            dto.Price,
            dto.CoverImageKey is not null
                ? objectStorageService.BuildDisplayUrl(dto.CoverImageKey)
                : null,
            dto.CategoryName,
            dto.Status,
            dto.Condition,
            dto.CreatedAt);
    }
}

public sealed record ProductResponse(
    Guid Id,
    string Title,
    string Slug,
    string Description,
    decimal Price,
    string? Condition,
    string Status,
    Guid CategoryId,
    string? CategoryName,
    IReadOnlyList<ProductImageResponse> Images,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record ProductImageResponse(
    Guid Id,
    string ObjectKey,
    string DisplayUrl,
    string? AltText,
    int SortOrder,
    bool IsPrimary);

public sealed record ProductListItemResponse(
    Guid Id,
    string Title,
    string Slug,
    decimal Price,
    string? CoverImageUrl,
    string? CategoryName,
    string Status,
    string? Condition,
    DateTime CreatedAt);
