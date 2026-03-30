using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Abstractions.Storage;
using SecondHandShop.Application.Contracts.Catalog;
using SecondHandShop.Application.Contracts.Common;
using SecondHandShop.Application.UseCases.Catalog;
using SecondHandShop.Domain.Enums;
using SecondHandShop.WebApi.Contracts;

namespace SecondHandShop.WebApi.Controllers;

[ApiController]
[Route("api/lord/products")]
[Authorize(Policy = "AdminOnly")]
public class AdminProductsController(
    IAdminCatalogService adminCatalogService,
    IProductRepository productRepository,
    IObjectStorageService objectStorageService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AdminProductListItem>>> ListAsync(
        [FromQuery] AdminProductQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await productRepository.ListPagedForAdminAsync(parameters, cancellationToken);

        var items = result.Items
            .Select(dto => new AdminProductListItem(
                dto.Id,
                dto.Title,
                dto.Slug,
                dto.Price,
                dto.Condition,
                dto.Status,
                dto.CategoryName,
                dto.ImageCount,
                dto.CoverImageKey is not null
                    ? objectStorageService.BuildDisplayUrl(dto.CoverImageKey)
                    : null,
                dto.IsFeatured,
                dto.FeaturedSortOrder,
                dto.CreatedAt,
                dto.UpdatedAt))
            .ToList();

        return Ok(new PagedResult<AdminProductListItem>(
            items, result.Page, result.PageSize, result.TotalCount));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateAdminProductRequest request, CancellationToken cancellationToken)
    {
        ProductCondition? condition = null;
        if (!string.IsNullOrWhiteSpace(request.Condition))
        {
            if (!TryParseCondition(request.Condition, out var parsed))
            {
                return BadRequest(new ErrorResponse($"Unsupported product condition '{request.Condition}'."));
            }
            condition = parsed;
        }

        var adminUserId = GetAdminUserId();
        var productId = await adminCatalogService.CreateProductAsync(
            new CreateProductRequest(
                request.Title,
                request.Slug,
                request.Description,
                request.Price,
                request.CategoryId,
                adminUserId,
                condition),
            cancellationToken);

        return Created($"/api/lord/products/{productId}", new CreateProductResponse(productId));
    }

    [HttpPut("{productId:guid}/status")]
    public async Task<IActionResult> UpdateStatusAsync(
        Guid productId,
        [FromBody] UpdateProductStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseStatus(request.Status, out var status))
        {
            return BadRequest(new ErrorResponse($"Unsupported product status '{request.Status}'."));
        }

        var adminUserId = GetAdminUserId();
        await adminCatalogService.UpdateProductStatusAsync(productId, status, adminUserId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{productId:guid}/featured")]
    public async Task<IActionResult> UpdateFeaturedAsync(
        Guid productId,
        [FromBody] UpdateAdminProductFeaturedRequest request,
        CancellationToken cancellationToken)
    {
        var adminUserId = GetAdminUserId();
        await adminCatalogService.UpdateProductFeaturedAsync(
            productId,
            request.IsFeatured,
            request.FeaturedSortOrder,
            adminUserId,
            cancellationToken);
        return NoContent();
    }

    [HttpPost("{productId:guid}/images/presigned-url")]
    public async Task<IActionResult> CreateImageUploadUrlAsync(
        Guid productId,
        [FromBody] CreateImageUploadUrlRequest request,
        CancellationToken cancellationToken)
    {
        var adminUserId = GetAdminUserId();
        var response = await adminCatalogService.CreateProductImageUploadUrlAsync(
            new CreateProductImageUploadUrlRequest(
                productId,
                request.FileName,
                request.ContentType,
                adminUserId),
            cancellationToken);

        return Ok(new CreateImageUploadUrlResponse(
            response.ObjectKey,
            response.PutUrl,
            response.ExpiresInSeconds));
    }

    [HttpPost("{productId:guid}/images")]
    public async Task<IActionResult> AddImageAsync(
        Guid productId,
        [FromBody] AddProductImageApiRequest request,
        CancellationToken cancellationToken)
    {
        var adminUserId = GetAdminUserId();
        await adminCatalogService.AddProductImageAsync(
            new AddProductImageRequest(
                productId,
                request.ObjectKey,
                request.AltText,
                request.SortOrder,
                request.IsPrimary,
                adminUserId),
            cancellationToken);

        return NoContent();
    }

    [HttpDelete("{productId:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> DeleteImageAsync(
        Guid productId,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        var adminUserId = GetAdminUserId();
        await adminCatalogService.DeleteProductImageAsync(productId, imageId, adminUserId, cancellationToken);
        return NoContent();
    }

    private Guid? GetAdminUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private static bool TryParseCondition(string value, out ProductCondition condition)
    {
        return Enum.TryParse(value, true, out condition) && Enum.IsDefined(condition);
    }

    private static bool TryParseStatus(string value, out ProductStatus status)
    {
        return Enum.TryParse(value, true, out status) && Enum.IsDefined(status);
    }
}

public sealed record CreateAdminProductRequest(
    string Title,
    string Slug,
    string Description,
    decimal Price,
    Guid CategoryId,
    string? Condition = null);

public sealed record CreateProductResponse(Guid Id);

public sealed record UpdateProductStatusRequest(
    string Status);

public sealed record UpdateAdminProductFeaturedRequest(
    bool IsFeatured,
    int? FeaturedSortOrder);

public sealed record CreateImageUploadUrlRequest(
    string FileName,
    string ContentType);

public sealed record CreateImageUploadUrlResponse(
    string ObjectKey,
    string PutUrl,
    int ExpiresInSeconds);

public sealed record AddProductImageApiRequest(
    string ObjectKey,
    string? AltText,
    int SortOrder,
    bool IsPrimary);

public sealed record AdminProductListItem(
    Guid Id,
    string Title,
    string Slug,
    decimal Price,
    string? Condition,
    string Status,
    string? CategoryName,
    int ImageCount,
    string? PrimaryImageUrl,
    bool IsFeatured,
    int? FeaturedSortOrder,
    DateTime CreatedAt,
    DateTime UpdatedAt);
