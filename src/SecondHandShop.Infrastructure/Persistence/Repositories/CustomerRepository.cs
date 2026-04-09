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

        var normalizedSource = parameters.PrimarySource?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSource)
            && Enum.TryParse<CustomerSource>(normalizedSource, ignoreCase: true, out var sourceFilter)
            && Enum.IsDefined(sourceFilter))
        {
            customersQuery = customersQuery.Where(c => c.PrimarySource == sourceFilter);
        }

        var totalCount = await customersQuery.CountAsync(cancellationToken);
        var page = parameters.SafePage;
        var pageSize = parameters.SafePageSize;

        var items = await customersQuery
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CustomerListItemDto(
                c.Id,
                c.Name,
                c.Email,
                c.PhoneNumber,
                c.Status.ToString(),
                c.PrimarySource.ToString(),
                (dbContext.ProductSales.Count(s => s.CustomerId == c.Id) > 0
                        || c.PrimarySource == CustomerSource.Sale
                    ? CustomerSource.Sale
                    : c.PrimarySource == CustomerSource.Manual
                        ? CustomerSource.Manual
                        : CustomerSource.Inquiry).ToString(),
                dbContext.Inquiries.Count(i => i.CustomerId == c.Id),
                dbContext.Inquiries.Where(i => i.CustomerId == c.Id).Max(i => (DateTime?)i.CreatedAt),
                dbContext.ProductSales.Count(s => s.CustomerId == c.Id),
                dbContext.ProductSales.Where(s => s.CustomerId == c.Id).Sum(s => (decimal?)s.FinalSoldPrice)
                    ?? 0m,
                dbContext.ProductSales.Where(s => s.CustomerId == c.Id).Max(s => (DateTime?)s.SoldAtUtc),
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
                dbContext.Inquiries.Where(i => i.CustomerId == c.Id).Max(i => (DateTime?)i.CreatedAt),
                dbContext.ProductSales.Count(s => s.CustomerId == c.Id),
                dbContext.ProductSales.Where(s => s.CustomerId == c.Id).Sum(s => (decimal?)s.FinalSoldPrice)
                    ?? 0m,
                dbContext.ProductSales.Where(s => s.CustomerId == c.Id).Max(s => (DateTime?)s.SoldAtUtc),
                c.LastContactAtUtc,
                c.CreatedAt,
                c.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await dbContext.Customers.AddAsync(customer, cancellationToken);
    }

}
