using Microsoft.EntityFrameworkCore;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Contracts.Catalog;
using SecondHandShop.Application.Contracts.Common;
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

    public async Task<PagedResult<ProductListItemDto>> ListPagedForPublicAsync(
        ProductQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Products
            .AsNoTracking()
            .Where(p => p.Status == ProductStatus.Available || p.Status == ProductStatus.Sold)
            .Where(p => dbContext.Categories.Any(c => c.Id == p.CategoryId && c.IsActive));

        var safeSearch = parameters.SafeSearch;
        if (safeSearch is not null)
        {
            query = query.Where(p => p.Title.Contains(safeSearch));
        }

        var safeCategory = parameters.SafeCategory;
        if (safeCategory is not null)
        {
            var categorySlug = safeCategory.ToLowerInvariant();
            query = query.Where(p =>
                dbContext.Categories.Any(c => c.Id == p.CategoryId && c.Slug == categorySlug));
        }

        if (parameters.MinPrice.HasValue)
            query = query.Where(p => p.Price >= parameters.MinPrice.Value);

        if (parameters.MaxPrice.HasValue)
            query = query.Where(p => p.Price <= parameters.MaxPrice.Value);

        if (!string.IsNullOrWhiteSpace(parameters.Status)
            && Enum.TryParse<ProductStatus>(parameters.Status, ignoreCase: true, out var statusEnum)
            && statusEnum is ProductStatus.Available or ProductStatus.Sold)
        {
            query = query.Where(p => p.Status == statusEnum);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var page = parameters.SafePage;
        var pageSize = parameters.SafePageSize;

        IOrderedQueryable<Product> orderedQuery = parameters.Sort switch
        {
            "price_asc" => query.OrderBy(p => p.Price).ThenByDescending(p => p.CreatedAt),
            "price_desc" => query.OrderByDescending(p => p.Price).ThenByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt),
        };

        var projected = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Slug,
                p.Price,
                p.Status,
                p.Condition,
                p.CreatedAt,
                CoverImageKey = dbContext.ProductImages
                    .Where(i => i.ProductId == p.Id)
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.SortOrder)
                    .Select(i => i.CloudStorageKey)
                    .FirstOrDefault(),
                CategoryName = dbContext.Categories
                    .Where(c => c.Id == p.CategoryId)
                    .Select(c => c.Name)
                    .FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);

        var items = projected
            .Select(p => new ProductListItemDto(
                p.Id,
                p.Title,
                p.Slug,
                p.Price,
                p.CoverImageKey,
                p.CategoryName,
                p.Status.ToString(),
                p.Condition?.ToString(),
                p.CreatedAt))
            .ToList();

        return new PagedResult<ProductListItemDto>(items, page, pageSize, totalCount);
    }

    public async Task<IReadOnlyList<Product>> ListForAdminAsync(
        ProductStatus? status,
        Guid? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Products.AsNoTracking();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (categoryId.HasValue)
            query = query.Where(x => x.CategoryId == categoryId.Value);

        return await query
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await dbContext.Products.AddAsync(product, cancellationToken);
    }
}
