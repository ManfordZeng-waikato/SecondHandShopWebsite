using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Contracts.Common;
using SecondHandShop.Application.Contracts.Customers;
using SecondHandShop.Application.Contracts.Sales;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Infrastructure.Persistence.Repositories;

public class InquiryRepository(SecondHandShopDbContext dbContext) : IInquiryRepository
{
    public async Task AcquireAntiSpamConcurrencyLocksAsync(
        string requestIpAddress,
        Guid productId,
        string? normalizedEmail,
        string messageHash,
        CancellationToken cancellationToken = default)
    {
        // Fixed lock order: message hash → IP+product → email+product (avoids deadlocks).
        await AcquirePostgresAdvisoryXactLockAsync("msg\u001f" + messageHash, cancellationToken);
        await AcquirePostgresAdvisoryXactLockAsync(
            "ip\u001f" + requestIpAddress + "\u001f" + productId,
            cancellationToken);
        if (!string.IsNullOrWhiteSpace(normalizedEmail))
        {
            await AcquirePostgresAdvisoryXactLockAsync(
                "em\u001f" + normalizedEmail + "\u001f" + productId,
                cancellationToken);
        }
    }

    public async Task AddAsync(Inquiry inquiry, CancellationToken cancellationToken = default)
    {
        await dbContext.Inquiries.AddAsync(inquiry, cancellationToken);
    }

    public async Task<int> CountRecentByIpAndProductAsync(
        string requestIpAddress,
        Guid productId,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Inquiries
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Where(x => x.RequestIpAddress == requestIpAddress)
            .Where(x => x.CreatedAt >= sinceUtc)
            .CountAsync(cancellationToken);
    }

    public async Task<int> CountRecentByEmailAndProductAsync(
        string email,
        Guid productId,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Inquiries
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .Where(x => x.Email == email)
            .Where(x => x.CreatedAt >= sinceUtc)
            .CountAsync(cancellationToken);
    }

    public async Task<bool> ExistsRecentByMessageHashAsync(
        string messageHash,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Inquiries
            .AsNoTracking()
            .Where(x => x.MessageHash == messageHash)
            .Where(x => x.CreatedAt >= sinceUtc)
            .AnyAsync(cancellationToken);
    }

    public async Task<DateTime?> GetIpCooldownUntilAsync(
        string requestIpAddress,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.InquiryIpCooldowns
            .AsNoTracking()
            .Where(x => x.IpAddress == requestIpAddress)
            .Select(x => (DateTime?)x.BlockedUntil)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpsertIpCooldownAsync(
        string requestIpAddress,
        DateTime blockedUntilUtc,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var cooldown = await dbContext.InquiryIpCooldowns
            .FirstOrDefaultAsync(x => x.IpAddress == requestIpAddress, cancellationToken);

        if (cooldown is null)
        {
            cooldown = InquiryIpCooldown.Create(requestIpAddress, blockedUntilUtc, updatedAtUtc);
            await dbContext.InquiryIpCooldowns.AddAsync(cooldown, cancellationToken);
            return;
        }

        cooldown.SetCooldown(blockedUntilUtc, updatedAtUtc);
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

    public async Task<IReadOnlyList<ProductInquiryOptionDto>> ListByProductIdForAdminAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await (
                from inquiry in dbContext.Inquiries.AsNoTracking()
                where inquiry.ProductId == productId
                join sale in dbContext.ProductSales.AsNoTracking()
                    on inquiry.Id equals sale.InquiryId into saleGroup
                from sale in saleGroup
                    .OrderByDescending(x => x.SoldAtUtc)
                    .Take(1)
                    .DefaultIfEmpty()
                orderby inquiry.CreatedAt descending, inquiry.Id descending
                select new ProductInquiryOptionDto(
                    inquiry.Id,
                    inquiry.CustomerName,
                    inquiry.Email,
                    inquiry.PhoneNumber,
                    inquiry.Message,
                    inquiry.CreatedAt,
                    sale != null ? sale.Id : null))
            .ToListAsync(cancellationToken);
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

    private static void DeriveAdvisoryLockKeys(string keyMaterial, out int k1, out int k2)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(keyMaterial));
        k1 = BitConverter.ToInt32(hash.AsSpan(0, 4));
        k2 = BitConverter.ToInt32(hash.AsSpan(4, 4));
    }

    private async Task AcquirePostgresAdvisoryXactLockAsync(
        string keyMaterial,
        CancellationToken cancellationToken)
    {
        DeriveAdvisoryLockKeys(keyMaterial, out var k1, out var k2);
        await dbContext.Database.ExecuteSqlAsync(
            $"""SELECT pg_advisory_xact_lock({k1}, {k2})""",
            cancellationToken);
    }
}
