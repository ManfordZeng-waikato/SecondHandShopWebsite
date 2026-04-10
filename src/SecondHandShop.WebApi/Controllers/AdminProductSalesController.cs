using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondHandShop.Application.Contracts.Sales;
using SecondHandShop.Application.UseCases.Sales;
using SecondHandShop.Domain.Enums;
using SecondHandShop.WebApi.Contracts;

namespace SecondHandShop.WebApi.Controllers;

[ApiController]
[Route("api/lord/products/{productId:guid}")]
[Authorize(Policy = "AdminFullAccess")]
public class AdminProductSalesController(IAdminSaleService adminSaleService) : ControllerBase
{
    /// <summary>
    /// Current (Completed) sale record for a product. Returns 404 if the product is not sold.
    /// </summary>
    [HttpGet("sale")]
    public async Task<ActionResult<ProductSaleDto>> GetCurrentAsync(Guid productId, CancellationToken cancellationToken)
    {
        var sale = await adminSaleService.GetCurrentSaleAsync(productId, cancellationToken);
        if (sale is null)
        {
            return NotFound(new ErrorResponse("No active sale record found for this product."));
        }

        return Ok(sale);
    }

    /// <summary>
    /// Full sale history for a product, newest first. Includes both completed and cancelled records.
    /// </summary>
    [HttpGet("sales")]
    public async Task<ActionResult<IReadOnlyList<ProductSaleDto>>> GetHistoryAsync(
        Guid productId,
        CancellationToken cancellationToken)
    {
        var history = await adminSaleService.GetSaleHistoryAsync(productId, cancellationToken);
        return Ok(history);
    }

    /// <summary>
    /// Mark a product as sold. Creates a new ProductSale row — never updates an existing one.
    /// </summary>
    [HttpPost("mark-sold")]
    public async Task<ActionResult<ProductSaleDto>> MarkSoldAsync(
        Guid productId,
        [FromBody] MarkProductSoldApiRequest request,
        CancellationToken cancellationToken)
    {
        var adminUserId = GetAdminUserId();
        var result = await adminSaleService.MarkAsSoldAsync(
            new MarkProductSoldRequest(
                productId,
                request.FinalSoldPrice,
                request.SoldAtUtc,
                adminUserId,
                request.CustomerId,
                request.InquiryId,
                request.BuyerName,
                request.BuyerPhone,
                request.BuyerEmail,
                request.PaymentMethod,
                request.Notes),
            cancellationToken);

        return Created($"/api/lord/products/{productId}/sale", result);
    }

    /// <summary>
    /// Revert a sold product back to Available. Marks the current sale as Cancelled with a
    /// reason; the historical buyer/price/time fields are preserved.
    /// </summary>
    [HttpPost("revert-sale")]
    public async Task<IActionResult> RevertSaleAsync(
        Guid productId,
        [FromBody] RevertProductSaleApiRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SaleCancellationReason>(request.Reason, true, out var reason)
            || !Enum.IsDefined(reason))
        {
            return BadRequest(new ErrorResponse($"Unsupported cancellation reason '{request.Reason}'."));
        }

        var adminUserId = GetAdminUserId();
        await adminSaleService.RevertSaleAsync(
            new RevertProductSaleRequest(productId, reason, request.CancellationNote, adminUserId),
            cancellationToken);

        return NoContent();
    }

    private Guid? GetAdminUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

public sealed record MarkProductSoldApiRequest
{
    [Range(0, double.MaxValue, ErrorMessage = "Final sold price cannot be negative.")]
    public decimal FinalSoldPrice { get; init; }

    public DateTime SoldAtUtc { get; init; }

    public Guid? CustomerId { get; init; }

    public Guid? InquiryId { get; init; }

    [MaxLength(200)]
    public string? BuyerName { get; init; }

    [MaxLength(40)]
    public string? BuyerPhone { get; init; }

    [MaxLength(256)]
    public string? BuyerEmail { get; init; }

    [MaxLength(50)]
    public string? PaymentMethod { get; init; }

    [MaxLength(2000)]
    public string? Notes { get; init; }
}

public sealed record RevertProductSaleApiRequest
{
    [Required]
    public string Reason { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? CancellationNote { get; init; }
}
