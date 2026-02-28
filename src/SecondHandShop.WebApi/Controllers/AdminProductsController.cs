using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandShop.Application.UseCases.Catalog;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.WebApi.Controllers;

[ApiController]
[Route("api/admin/products")]
public class AdminProductsController(IAdminCatalogService adminCatalogService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateAdminProductRequest request, CancellationToken cancellationToken)
    {
        if (!TryParseCondition(request.Condition, out var condition))
        {
            return BadRequest(new ErrorResponse($"Unsupported product condition '{request.Condition}'."));
        }

        try
        {
            var productId = await adminCatalogService.CreateProductAsync(
                new CreateProductRequest(
                    request.Title,
                    request.Slug,
                    request.Description,
                    request.Price,
                    condition,
                    request.CategoryId,
                    request.AdminUserId),
                cancellationToken);

            return Created($"/api/admin/products/{productId}", new CreateProductResponse(productId));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse(ex.Message));
        }
        catch (DbUpdateException)
        {
            return Conflict(new ErrorResponse("Concurrent update detected. The product may already have a primary image."));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse(ex.Message));
        }
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

        try
        {
            await adminCatalogService.UpdateProductStatusAsync(productId, status, request.AdminUserId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse(ex.Message));
        }
    }

    [HttpPost("{productId:guid}/images/presigned-url")]
    public async Task<IActionResult> CreateImageUploadUrlAsync(
        Guid productId,
        [FromBody] CreateImageUploadUrlRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await adminCatalogService.CreateProductImageUploadUrlAsync(
                new CreateProductImageUploadUrlRequest(
                    productId,
                    request.FileName,
                    request.ContentType,
                    request.AdminUserId),
                cancellationToken);

            return Ok(new CreateImageUploadUrlResponse(
                response.ObjectKey,
                response.PutUrl,
                response.ExpiresInSeconds));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse(ex.Message));
        }
    }

    [HttpPost("{productId:guid}/images")]
    public async Task<IActionResult> AddImageAsync(
        Guid productId,
        [FromBody] AddProductImageApiRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await adminCatalogService.AddProductImageAsync(
                new AddProductImageRequest(
                    productId,
                    request.ObjectKey,
                    request.AltText,
                    request.SortOrder,
                    request.IsPrimary,
                    request.AdminUserId),
                cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse(ex.Message));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("{productId:guid}/images/{imageId:guid}")]
    public async Task<IActionResult> DeleteImageAsync(
        Guid productId,
        Guid imageId,
        [FromQuery] Guid? adminUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            await adminCatalogService.DeleteProductImageAsync(productId, imageId, adminUserId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse(ex.Message));
        }
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
    string Condition,
    Guid CategoryId,
    Guid? AdminUserId);

public sealed record CreateProductResponse(Guid Id);

public sealed record UpdateProductStatusRequest(
    string Status,
    Guid? AdminUserId);

public sealed record CreateImageUploadUrlRequest(
    string FileName,
    string ContentType,
    Guid? AdminUserId);

public sealed record CreateImageUploadUrlResponse(
    string ObjectKey,
    string PutUrl,
    int ExpiresInSeconds);

public sealed record AddProductImageApiRequest(
    string ObjectKey,
    string? AltText,
    int SortOrder,
    bool IsPrimary,
    Guid? AdminUserId);
