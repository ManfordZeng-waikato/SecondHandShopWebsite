using SecondHandShop.Domain.Entities;
using SecondHandShop.Application.Contracts.Common;
using SecondHandShop.Application.Contracts.Customers;

namespace SecondHandShop.Application.Abstractions.Persistence;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<Customer?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default);
    Task<PagedResult<CustomerListItemDto>> ListPagedForAdminAsync(
        AdminCustomerQueryParameters parameters,
        CancellationToken cancellationToken = default);
    Task<CustomerDetailDto?> GetDetailForAdminAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
}
