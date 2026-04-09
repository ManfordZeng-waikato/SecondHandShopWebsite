using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SecondHandShop.Application.Contracts.Inquiries;
using SecondHandShop.Application.UseCases.Inquiries;
using SecondHandShop.Domain.Common;
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

public sealed record CreateInquiryRequest : IValidatableObject
{
    public required Guid ProductId { get; init; }

    [MaxLength(120)]
    public string? CustomerName { get; init; }

    [MaxLength(256)]
    public string? Email { get; init; }

    [MaxLength(40)]
    [RegularExpression(@"^[0-9+\-\s()]+$", ErrorMessage = "Phone number can only contain digits, +, -, spaces, and parentheses.")]
    public string? PhoneNumber { get; init; }

    [Required]
    [MaxLength(3000)]
    public required string Message { get; init; }

    [Required]
    public required string TurnstileToken { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(Email) && !EmailAddressSyntaxValidator.IsValid(Email))
        {
            yield return new ValidationResult("Invalid email format.", [nameof(Email)]);
        }
    }
}

public sealed record CreateInquiryResponse(Guid InquiryId);
