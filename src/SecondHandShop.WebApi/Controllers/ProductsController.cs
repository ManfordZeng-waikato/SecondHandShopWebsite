using Microsoft.AspNetCore.Mvc;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Abstractions.Storage;

namespace SecondHandShop.WebApi.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IProductImageRepository productImageRepository,
    IObjectStorageService objectStorageService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> ListAsync(
        [FromQuery] Guid? categoryId,
        CancellationToken cancellationToken)
    {
        var products = await productRepository.ListForPublicAsync(categoryId, cancellationToken);
        var categories = await categoryRepository.ListActiveAsync(cancellationToken);
        var categoryMap = categories.ToDictionary(x => x.Id, x => x.Name);

        var response = new List<ProductResponse>(products.Count);
        foreach (var product in products)
        {
            var images = await productImageRepository.ListByProductIdAsync(product.Id, cancellationToken);
            response.Add(ToProductResponse(product, images, categoryMap));
        }

        return Ok(response);
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<ProductResponse>> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();
        var product = await productRepository.GetBySlugAsync(normalizedSlug, cancellationToken);
        if (product is null)
        {
            return NotFound(new ErrorResponse("Product was not found."));
        }

        var categories = await categoryRepository.ListActiveAsync(cancellationToken);
        var categoryMap = categories.ToDictionary(x => x.Id, x => x.Name);
        var images = await productImageRepository.ListByProductIdAsync(product.Id, cancellationToken);
        return Ok(ToProductResponse(product, images, categoryMap));
    }

    private ProductResponse ToProductResponse(
        Domain.Entities.Product product,
        IReadOnlyList<Domain.Entities.ProductImage> images,
        IReadOnlyDictionary<Guid, string> categoryMap)
    {
        categoryMap.TryGetValue(product.CategoryId, out var categoryName);

        return new ProductResponse(
            product.Id,
            product.Title,
            product.Slug,
            product.Description,
            product.Price,
            product.Condition.ToString(),
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
}

public sealed record ProductResponse(
    Guid Id,
    string Title,
    string Slug,
    string Description,
    decimal Price,
    string Condition,
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
