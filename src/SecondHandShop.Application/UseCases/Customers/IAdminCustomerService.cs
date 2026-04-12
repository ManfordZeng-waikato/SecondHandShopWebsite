using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Application.UseCases.Customers;

public interface IAdminCustomerService
{
    Task UpdateCustomerAsync(
        Guid customerId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateCustomerAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record UpdateCustomerRequest(
    string? Name,
    string? PhoneNumber,
    CustomerStatus? Status,
    string? Notes);

public sealed record CreateCustomerRequest(
    string? Name,
    string? Email,
    string? PhoneNumber,
    CustomerStatus? Status,
    string? Notes);
