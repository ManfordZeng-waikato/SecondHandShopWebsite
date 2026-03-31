using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondHandShop.Application.Contracts.Sales;
using SecondHandShop.Application.UseCases.Sales;
using SecondHandShop.WebApi.Contracts;

namespace SecondHandShop.WebApi.Controllers;

[ApiController]
[Route("api/lord/products/{productId:guid}/sale")]
[Authorize(Policy = "AdminFullAccess")]
public class AdminProductSalesController(IAdminSaleService adminSaleService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProductSaleDto>> GetAsync(Guid productId, CancellationToken cancellationToken)
    {
        var sale = await adminSaleService.GetByProductIdAsync(productId, cancellationToken);
        if (sale is null)
        {
            return NotFound(new ErrorResponse("No sale record found for this product."));
        }

        return Ok(sale);
    }

    [HttpPost]
    public async Task<ActionResult<ProductSaleDto>> CreateAsync(
        Guid productId,
        [FromBody] SaveProductSaleApiRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await adminSaleService.GetByProductIdAsync(productId, cancellationToken);
        if (existing is not null)
        {
            return Conflict(new ErrorResponse("A sale record already exists for this product. Use PUT to update."));
        }

        var adminUserId = GetAdminUserId();
        var result = await adminSaleService.SaveAsync(
            new SaveProductSaleRequest(
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

    [HttpPut]
    public async Task<ActionResult<ProductSaleDto>> UpdateAsync(
        Guid productId,
        [FromBody] SaveProductSaleApiRequest request,
        CancellationToken cancellationToken)
    {
        var adminUserId = GetAdminUserId();
        var result = await adminSaleService.SaveAsync(
            new SaveProductSaleRequest(
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

        return Ok(result);
    }

    private Guid? GetAdminUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

public sealed record SaveProductSaleApiRequest
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
