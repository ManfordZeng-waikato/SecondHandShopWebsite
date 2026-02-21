using Microsoft.EntityFrameworkCore;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Infrastructure.Persistence.Repositories;

public class ProductRepository(SecondHandShopDbContext dbContext) : IProductRepository
{
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await dbContext.Products.FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> ListForPublicAsync(Guid? categoryId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Products
            .AsNoTracking()
            .Where(x => x.Status == ProductStatus.Available || x.Status == ProductStatus.Sold)
            .Where(x => dbContext.Categories.Any(c => c.Id == x.CategoryId && c.IsActive));

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        return await query
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> ListForAdminAsync(ProductStatus? status, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Products.AsNoTracking();
        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        return await query
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await dbContext.Products.AddAsync(product, cancellationToken);
    }
}
