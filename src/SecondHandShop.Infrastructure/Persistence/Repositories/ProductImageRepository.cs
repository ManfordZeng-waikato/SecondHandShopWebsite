using Microsoft.EntityFrameworkCore;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Infrastructure.Persistence.Repositories;

public class ProductImageRepository(SecondHandShopDbContext dbContext) : IProductImageRepository
{
    public async Task<IReadOnlyList<ProductImage>> ListByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ProductImages
            .AsNoTracking()
            .Where(x => x.ProductId == productId)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ProductImage productImage, CancellationToken cancellationToken = default)
    {
        await dbContext.ProductImages.AddAsync(productImage, cancellationToken);
    }
}
