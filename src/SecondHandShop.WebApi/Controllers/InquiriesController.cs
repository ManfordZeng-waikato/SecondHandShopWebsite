using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using SecondHandShop.Application.Contracts.Inquiries;
using SecondHandShop.Application.UseCases.Inquiries;

namespace SecondHandShop.WebApi.Controllers;

[ApiController]
[Route("api/inquiries")]
public class InquiriesController(IInquiryService inquiryService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateInquiryRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateInquiryCommand(
            request.ProductId,
            request.CustomerName,
            request.Email,
            request.PhoneNumber,
            request.Message);

        try
        {
            var inquiryId = await inquiryService.CreateInquiryAsync(command, cancellationToken);
            return Created($"/api/inquiries/{inquiryId}", new CreateInquiryResponse(inquiryId));
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
}

public sealed record CreateInquiryResponse(Guid InquiryId);

public sealed record ErrorResponse(string Message);
