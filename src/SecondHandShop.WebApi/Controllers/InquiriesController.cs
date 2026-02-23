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

public sealed record CreateInquiryRequest(
    Guid ProductId,
    string? CustomerName,
    string? Email,
    string? PhoneNumber,
    string Message);

public sealed record CreateInquiryResponse(Guid InquiryId);

public sealed record ErrorResponse(string Message);
