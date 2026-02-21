using Microsoft.EntityFrameworkCore;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Infrastructure.Persistence.Repositories;

public class CategoryRepository(SecondHandShopDbContext dbContext) : ICategoryRepository
{
    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories.FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);
    }

    public async Task<IReadOnlyList<Category>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        await dbContext.Categories.AddAsync(category, cancellationToken);
    }
}
