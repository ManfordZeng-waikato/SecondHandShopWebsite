using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Application.Abstractions.Persistence;

public interface IInquiryRepository
{
    Task AddAsync(Inquiry inquiry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Inquiry>> ListPendingEmailAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<Inquiry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
