using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SecondHandShop.Application.Contracts.Inquiries;
using SecondHandShop.Application.UseCases.Inquiries;
using SecondHandShop.WebApi.Contracts;

namespace SecondHandShop.WebApi.Controllers;

[ApiController]
[Route("api/inquiries")]
public class InquiriesController(IInquiryService inquiryService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateInquiryRequest request, CancellationToken cancellationToken)
    {
        var requestIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var command = new CreateInquiryCommand(
            request.ProductId,
            request.CustomerName,
            request.Email,
            request.PhoneNumber,
            request.Message,
            request.TurnstileToken,
            requestIpAddress);

        var inquiryId = await inquiryService.CreateInquiryAsync(command, cancellationToken);
        return Created($"/api/inquiries/{inquiryId}", new CreateInquiryResponse(inquiryId));
    }
}

public sealed record CreateInquiryRequest
{
    public required Guid ProductId { get; init; }

    [MaxLength(120)]
    public string? CustomerName { get; init; }

    [MaxLength(256)]
    [RegularExpression(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", ErrorMessage = "Invalid email format.")]
    public string? Email { get; init; }

    [MaxLength(40)]
    [RegularExpression(@"^[0-9+\-\s()]+$", ErrorMessage = "Phone number can only contain digits, +, -, spaces, and parentheses.")]
    public string? PhoneNumber { get; init; }

    [Required]
    [MaxLength(3000)]
    public required string Message { get; init; }

    [Required]
    public required string TurnstileToken { get; init; }
}

public sealed record CreateInquiryResponse(Guid InquiryId);
