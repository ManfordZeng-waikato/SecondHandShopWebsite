using SecondHandShop.Domain.Entities;
using SecondHandShop.Application.Contracts.Common;
using SecondHandShop.Application.Contracts.Customers;
using SecondHandShop.Application.Contracts.Sales;

namespace SecondHandShop.Application.Abstractions.Persistence;

public interface IInquiryRepository
{
    /// <summary>
    /// Acquires PostgreSQL transaction-scoped advisory locks so that anti-spam count checks and
    /// subsequent inserts for the same logical keys cannot interleave. Must be called only while a
    /// database transaction is active. Locks are acquired in a fixed order to avoid deadlocks.
    /// </summary>
    Task AcquireAntiSpamConcurrencyLocksAsync(
        string requestIpAddress,
        Guid productId,
        string? normalizedEmail,
        string messageHash,
        CancellationToken cancellationToken = default);

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
    Task<DateTime?> GetIpCooldownUntilAsync(
        string requestIpAddress,
        CancellationToken cancellationToken = default);
    Task UpsertIpCooldownAsync(
        string requestIpAddress,
        DateTime blockedUntilUtc,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Inquiry>> ListPendingEmailAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<Inquiry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductInquiryOptionDto>> ListByProductIdForAdminAsync(
        Guid productId,
        CancellationToken cancellationToken = default);
    Task<PagedResult<CustomerInquiryItemDto>> ListPagedByCustomerIdForAdminAsync(
        Guid customerId,
        CustomerInquiryQueryParameters parameters,
        CancellationToken cancellationToken = default);
}
