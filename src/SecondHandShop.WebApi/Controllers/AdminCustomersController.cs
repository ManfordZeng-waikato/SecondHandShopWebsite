using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Contracts.Common;
using SecondHandShop.Application.Contracts.Customers;
using SecondHandShop.Application.Contracts.Sales;
using SecondHandShop.Application.UseCases.Customers;
using SecondHandShop.Application.UseCases.Sales;
using SecondHandShop.Domain.Enums;
using SecondHandShop.WebApi.Contracts;

namespace SecondHandShop.WebApi.Controllers;

[ApiController]
[Route("api/lord/customers")]
[Authorize(Policy = "AdminFullAccess")]
public class AdminCustomersController(
    ICustomerRepository customerRepository,
    IInquiryRepository inquiryRepository,
    IAdminCustomerService adminCustomerService,
    IAdminSaleService adminSaleService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerListItemDto>>> ListAsync(
        [FromQuery] AdminCustomerQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var customers = await customerRepository.ListPagedForAdminAsync(parameters, cancellationToken);
        return Ok(customers);
    }

    [HttpGet("{customerId:guid}")]
    public async Task<ActionResult<CustomerDetailDto>> GetDetailAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetDetailForAdminAsync(customerId, cancellationToken);
        if (customer is null)
        {
            return NotFound(new ErrorResponse($"Customer '{customerId}' was not found."));
        }

        return Ok(customer);
    }

    [HttpGet("{customerId:guid}/inquiries")]
    public async Task<ActionResult<PagedResult<CustomerInquiryItemDto>>> ListInquiriesAsync(
        Guid customerId,
        [FromQuery] CustomerInquiryQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer is null)
        {
            return NotFound(new ErrorResponse($"Customer '{customerId}' was not found."));
        }

        var inquiries = await inquiryRepository.ListPagedByCustomerIdForAdminAsync(
            customerId,
            parameters,
            cancellationToken);

        return Ok(inquiries);
    }

    [HttpGet("{customerId:guid}/sales")]
    public async Task<ActionResult<IReadOnlyList<CustomerSaleItemDto>>> ListSalesAsync(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer is null)
        {
            return NotFound(new ErrorResponse($"Customer '{customerId}' was not found."));
        }

        var sales = await adminSaleService.ListByCustomerIdAsync(customerId, cancellationToken);
        return Ok(sales);
    }

    [HttpPatch("{customerId:guid}")]
    public async Task<IActionResult> UpdateAsync(
        Guid customerId,
        [FromBody] UpdateCustomerApiRequest request,
        CancellationToken cancellationToken)
    {
        CustomerStatus? status = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!TryParseStatus(request.Status, out var parsedStatus))
            {
                return BadRequest(new ErrorResponse($"Unsupported customer status '{request.Status}'."));
            }

            status = parsedStatus;
        }

        await adminCustomerService.UpdateCustomerAsync(
            customerId,
            new UpdateCustomerRequest(
                request.Name,
                request.PhoneNumber,
                status,
                request.Notes),
            cancellationToken);
        return NoContent();
    }

    private static bool TryParseStatus(string value, out CustomerStatus status)
    {
        return Enum.TryParse(value, true, out status) && Enum.IsDefined(status);
    }
}

public sealed record UpdateCustomerApiRequest
{
    [MaxLength(120)]
    public string? Name { get; init; }

    [MaxLength(40)]
    [RegularExpression(@"^[0-9+\-\s()]*$", ErrorMessage = "Phone number can only contain digits, +, -, spaces, and parentheses.")]
    public string? PhoneNumber { get; init; }

    [MaxLength(50)]
    public string? Status { get; init; }

    [MaxLength(2000)]
    public string? Notes { get; init; }
}
