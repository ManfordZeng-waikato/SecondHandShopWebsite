using Microsoft.EntityFrameworkCore;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Contracts.Common;
using SecondHandShop.Application.Contracts.Customers;
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

    public async Task<PagedResult<CustomerInquiryItemDto>> ListPagedByCustomerIdForAdminAsync(
        Guid customerId,
        CustomerInquiryQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var inquiryQuery = dbContext.Inquiries
            .AsNoTracking()
            .Where(x => x.CustomerId == customerId);

        var totalCount = await inquiryQuery.CountAsync(cancellationToken);
        var page = parameters.SafePage;
        var pageSize = parameters.SafePageSize;

        var items = await (
                from inquiry in inquiryQuery
                join product in dbContext.Products.AsNoTracking()
                    on inquiry.ProductId equals product.Id into productGroup
                from product in productGroup.DefaultIfEmpty()
                orderby inquiry.CreatedAt descending, inquiry.Id descending
                select new CustomerInquiryItemDto(
                    inquiry.Id,
                    inquiry.ProductId,
                    product != null ? product.Title : null,
                    product != null ? product.Slug : null,
                    inquiry.Message,
                    inquiry.EmailDeliveryStatus.ToString(),
                    inquiry.CreatedAt))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CustomerInquiryItemDto>(items, page, pageSize, totalCount);
    }
}
