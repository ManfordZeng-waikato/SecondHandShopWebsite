using Microsoft.EntityFrameworkCore;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Contracts.Sales;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Infrastructure.Persistence.Repositories;

public class ProductSaleRepository(SecondHandShopDbContext dbContext) : IProductSaleRepository
{
    public async Task<ProductSale?> GetCurrentByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ProductSales
            .FirstOrDefaultAsync(
                s => s.ProductId == productId && s.Status == SaleRecordStatus.Completed,
                cancellationToken);
    }

    public async Task<ProductSale?> GetByIdAsync(Guid saleId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ProductSales
            .FirstOrDefaultAsync(s => s.Id == saleId, cancellationToken);
    }

    public async Task<IReadOnlyList<ProductSale>> ListHistoryByProductIdAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ProductSales
            .AsNoTracking()
            .Where(s => s.ProductId == productId)
            .OrderByDescending(s => s.SoldAtUtc)
            .ThenByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerSaleItemDto>> ListByCustomerIdAsync(
        Guid customerId, CancellationToken cancellationToken = default)
    {
        // Only completed sales show up on the customer profile — cancelled ones are noise there.
        var rows = await dbContext.ProductSales
            .Where(s => s.CustomerId == customerId && s.Status == SaleRecordStatus.Completed)
            .Join(
                dbContext.Products,
                sale => sale.ProductId,
                product => product.Id,
                (sale, product) => new
                {
                    sale.Id,
                    sale.ProductId,
                    product.Title,
                    product.Slug,
                    sale.FinalSoldPrice,
                    sale.SoldAtUtc,
                    sale.PaymentMethod,
                    sale.InquiryId
                })
            .OrderByDescending(x => x.SoldAtUtc)
            .ToListAsync(cancellationToken);

        return rows.Select(r => new CustomerSaleItemDto(
            r.Id,
            r.ProductId,
            r.Title,
            r.Slug,
            r.FinalSoldPrice,
            r.SoldAtUtc,
            r.PaymentMethod?.ToString(),
            r.InquiryId)).ToList();
    }

    public async Task AddAsync(ProductSale sale, CancellationToken cancellationToken = default)
    {
        await dbContext.ProductSales.AddAsync(sale, cancellationToken);
    }
}
