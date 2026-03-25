using SecondHandShop.Domain.Entities;
using SecondHandShop.Application.Contracts.Common;
using SecondHandShop.Application.Contracts.Customers;

namespace SecondHandShop.Application.Abstractions.Persistence;

public interface IInquiryRepository
{
    Task AddAsync(Inquiry inquiry, CancellationToken cancellationToken = default);
    Task<int> CountRecentByIpAndProductAsync(
        string requestIpAddress,
        Guid productId,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default);
    Task<int> CountRecentByEmailAndProductAsync(
        string email,
        Guid productId,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default);
    Task<bool> ExistsRecentByMessageHashAsync(
        string messageHash,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Inquiry>> ListPendingEmailAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<Inquiry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<CustomerInquiryItemDto>> ListPagedByCustomerIdForAdminAsync(
        Guid customerId,
        CustomerInquiryQueryParameters parameters,
        CancellationToken cancellationToken = default);
}
