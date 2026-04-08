using Microsoft.EntityFrameworkCore;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Contracts.Common;
using SecondHandShop.Application.Contracts.Customers;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Infrastructure.Persistence.Repositories;

public class CustomerRepository(SecondHandShopDbContext dbContext) : ICustomerRepository
{
    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Customers
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await dbContext.Customers
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
    }

    public async Task<Customer?> GetByPhoneNumberAsync(string phoneNumber, CancellationToken cancellationToken = default)
    {
        return await dbContext.Customers
            .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber, cancellationToken);
    }

    public async Task<PagedResult<CustomerListItemDto>> ListPagedForAdminAsync(
        AdminCustomerQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var customersQuery = dbContext.Customers.AsNoTracking();
        var safeSearch = parameters.SafeSearch;

        if (safeSearch is not null)
        {
            customersQuery = customersQuery.Where(c =>
                (c.Name != null && c.Name.Contains(safeSearch))
                || (c.Email != null && c.Email.Contains(safeSearch))
                || (c.PhoneNumber != null && c.PhoneNumber.Contains(safeSearch)));
        }

        var normalizedStatus = parameters.Status?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedStatus)
            && Enum.TryParse<CustomerStatus>(normalizedStatus, ignoreCase: true, out var statusFilter)
            && Enum.IsDefined(statusFilter))
        {
            customersQuery = customersQuery.Where(c => c.Status == statusFilter);
        }

        var projectedQuery = customersQuery.Select(c => new CustomerAdminProjection
        {
            Id = c.Id,
            Name = c.Name,
            Email = c.Email,
            Phone = c.PhoneNumber,
            Status = c.Status,
            PrimarySource = c.PrimarySource,
            LastContactAtUtc = c.LastContactAtUtc,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            InquiryCount = dbContext.Inquiries.Count(i => i.CustomerId == c.Id),
            LastInquiryAt = dbContext.Inquiries
                .Where(i => i.CustomerId == c.Id)
                .Max(i => (DateTime?)i.CreatedAt),
            PurchaseCount = dbContext.ProductSales.Count(s => s.CustomerId == c.Id),
            TotalSpent = dbContext.ProductSales
                .Where(s => s.CustomerId == c.Id)
                .Sum(s => (decimal?)s.FinalSoldPrice) ?? 0m,
            LastPurchaseAtUtc = dbContext.ProductSales
                .Where(s => s.CustomerId == c.Id)
                .Max(s => (DateTime?)s.SoldAtUtc)
        });

        var totalCount = await customersQuery.CountAsync(cancellationToken);
        var page = parameters.SafePage;
        var pageSize = parameters.SafePageSize;

        var items = await projectedQuery
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerListItemDto(
                c.Id,
                c.Name,
                c.Email,
                c.Phone,
                c.Status.ToString(),
                c.PrimarySource.ToString(),
                c.InquiryCount,
                c.LastInquiryAt,
                c.PurchaseCount,
                c.TotalSpent,
                c.LastPurchaseAtUtc,
                c.LastContactAtUtc,
                c.CreatedAt,
                c.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<CustomerListItemDto>(items, page, pageSize, totalCount);
    }

    public async Task<CustomerDetailDto?> GetDetailForAdminAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Customers
            .AsNoTracking()
            .Where(c => c.Id == customerId)
            .Select(c => new CustomerDetailDto(
                c.Id,
                c.Name,
                c.Email,
                c.PhoneNumber,
                c.Status.ToString(),
                c.PrimarySource.ToString(),
                c.Notes,
                dbContext.Inquiries.Count(i => i.CustomerId == c.Id),
                dbContext.Inquiries
                    .Where(i => i.CustomerId == c.Id)
                    .Max(i => (DateTime?)i.CreatedAt),
                dbContext.ProductSales.Count(s => s.CustomerId == c.Id),
                dbContext.ProductSales
                    .Where(s => s.CustomerId == c.Id)
                    .Sum(s => (decimal?)s.FinalSoldPrice) ?? 0m,
                dbContext.ProductSales
                    .Where(s => s.CustomerId == c.Id)
                    .Max(s => (DateTime?)s.SoldAtUtc),
                c.LastContactAtUtc,
                c.CreatedAt,
                c.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await dbContext.Customers.AddAsync(customer, cancellationToken);
    }

    private sealed class CustomerAdminProjection
    {
        public required Guid Id { get; init; }
        public string? Name { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public required CustomerStatus Status { get; init; }
        public required CustomerSource PrimarySource { get; init; }
        public DateTime? LastContactAtUtc { get; init; }
        public required DateTime CreatedAt { get; init; }
        public required DateTime UpdatedAt { get; init; }
        public required int InquiryCount { get; init; }
        public DateTime? LastInquiryAt { get; init; }
        public required int PurchaseCount { get; init; }
        public required decimal TotalSpent { get; init; }
        public DateTime? LastPurchaseAtUtc { get; init; }
    }
}
