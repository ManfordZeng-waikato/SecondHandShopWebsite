using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Application.UseCases.Customers;

public interface ICustomerResolutionService
{
    /// <summary>
    /// Finds an existing customer by email (primary) or phone (secondary),
    /// or creates a new one. Applies cautious merge on existing customers.
    /// </summary>
    Task<Customer> GetOrCreateCustomerAsync(
        string? name,
        string? email,
        string? phoneNumber,
        CustomerSource source,
        CancellationToken cancellationToken = default);
}
