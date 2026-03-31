using Microsoft.EntityFrameworkCore;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Contracts.Sales;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Infrastructure.Persistence.Repositories;

public class ProductSaleRepository(SecondHandShopDbContext dbContext) : IProductSaleRepository
{
    public async Task<ProductSale?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ProductSales
            .FirstOrDefaultAsync(s => s.ProductId == productId, cancellationToken);
    }

    public async Task<IReadOnlyList<CustomerSaleItemDto>> ListByCustomerIdAsync(
        Guid customerId, CancellationToken cancellationToken = default)
    {
        var rows = await dbContext.ProductSales
            .Where(s => s.CustomerId == customerId)
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
