using Microsoft.EntityFrameworkCore;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Infrastructure.Persistence.Repositories;

public class InquiryRepository(SecondHandShopDbContext dbContext) : IInquiryRepository
{
    public async Task AddAsync(Inquiry inquiry, CancellationToken cancellationToken = default)
    {
        await dbContext.Inquiries.AddAsync(inquiry, cancellationToken);
    }

    public async Task<IReadOnlyList<Inquiry>> ListPendingEmailAsync(DateTime utcNow, CancellationToken cancellationToken = default)
    {
        return await dbContext.Inquiries
            .Where(x => x.EmailDeliveryStatus == EmailDeliveryStatus.Pending)
            .Where(x => x.NextRetryAt == null || x.NextRetryAt <= utcNow)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Inquiry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Inquiries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
