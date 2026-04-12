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

    public async Task<Product?> GetByIdWithCategoriesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Products
            .Include(x => x.ProductCategories)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await dbContext.Products.FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);
    }

    public async Task<Product?> GetPublicBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Where(x => x.Slug == slug)
            .Where(x => x.Status == ProductStatus.Available || x.Status == ProductStatus.Sold)
            .Where(x => dbContext.Categories.Any(c => c.Id == x.CategoryId && c.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Product?> GetPublicByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Where(x => x.Status == ProductStatus.Available || x.Status == ProductStatus.Sold)
            .Where(x => dbContext.Categories.Any(c => c.Id == x.CategoryId && c.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
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
            var activeCategories = await dbContext.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .Select(c => new CategoryHierarchyNode(c.Id, c.Slug, c.ParentId))
                .ToListAsync(cancellationToken);

            var selectedCategory = activeCategories
                .FirstOrDefault(c => c.Slug == categorySlug);

            if (selectedCategory is null)
            {
                query = query.Where(_ => false);
            }
            else
            {
                var descendantCategoryIds = CollectDescendantCategoryIds(selectedCategory.Id, activeCategories);
                query = query.Where(p =>
                    dbContext.ProductCategories.Any(pc =>
                        pc.ProductId == p.Id &&
                        descendantCategoryIds.Contains(pc.CategoryId)));
            }
        }

        var safeCategoryIds = parameters.SafeCategoryIds;
        if (safeCategoryIds.Count > 0)
        {
            query = query.Where(p =>
                dbContext.ProductCategories.Any(pc =>
                    pc.ProductId == p.Id &&
                    safeCategoryIds.Contains(pc.CategoryId)));
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

        var pageSlice = orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var projected = await (
            from p in pageSlice
            join c in dbContext.Categories.AsNoTracking() on p.CategoryId equals c.Id into cg
            from category in cg.DefaultIfEmpty()
            select new
            {
                p.Id,
                p.Title,
                p.Slug,
                p.Price,
                p.Status,
                p.Condition,
                p.CreatedAt,
                p.CoverImageKey,
                CategoryName = category != null ? category.Name : null,
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

    public async Task<IReadOnlyList<ProductListItemDto>> ListFeaturedForPublicAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 24);

        var query = dbContext.Products
            .AsNoTracking()
            .Where(p => p.IsFeatured)
            .Where(p => p.Status == ProductStatus.Available)
            .Where(p => dbContext.Categories.Any(c => c.Id == p.CategoryId && c.IsActive));

        var limitedQuery = query
            .OrderBy(p => p.FeaturedSortOrder.HasValue ? 0 : 1)
            .ThenBy(p => p.FeaturedSortOrder)
            .ThenByDescending(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Take(safeLimit);

        var projected = await (
            from p in limitedQuery
            join c in dbContext.Categories.AsNoTracking() on p.CategoryId equals c.Id into cg
            from category in cg.DefaultIfEmpty()
            select new
            {
                p.Id,
                p.Title,
                p.Slug,
                p.Price,
                p.Status,
                p.Condition,
                p.CreatedAt,
                p.CoverImageKey,
                CategoryName = category != null ? category.Name : null,
            })
            .ToListAsync(cancellationToken);

        return projected
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
    }

    public async Task<PagedResult<AdminProductListItemDto>> ListPagedForAdminAsync(
        AdminProductQueryParameters parameters,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Products.AsNoTracking();

        var search = parameters.SafeSearch;
        if (search is not null)
            query = query.Where(x => x.Title.Contains(search));

        if (!string.IsNullOrWhiteSpace(parameters.Status)
            && Enum.TryParse<ProductStatus>(parameters.Status, ignoreCase: true, out var statusEnum))
        {
            query = query.Where(x => x.Status == statusEnum);
        }

        if (parameters.CategoryId.HasValue)
        {
            var activeCategories = await dbContext.Categories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .Select(c => new CategoryHierarchyNode(c.Id, c.Slug, c.ParentId))
                .ToListAsync(cancellationToken);

            var selectedCategoryId = parameters.CategoryId.Value;
            if (!activeCategories.Any(c => c.Id == selectedCategoryId))
            {
                query = query.Where(_ => false);
            }
            else
            {
                var descendantCategoryIds = CollectDescendantCategoryIds(selectedCategoryId, activeCategories);
                query = query.Where(p =>
                    dbContext.ProductCategories.Any(pc =>
                        pc.ProductId == p.Id &&
                        descendantCategoryIds.Contains(pc.CategoryId)));
            }
        }

        if (parameters.IsFeatured.HasValue)
            query = query.Where(x => x.IsFeatured == parameters.IsFeatured.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var page = parameters.SafePage;
        var pageSize = parameters.SafePageSize;

        IOrderedQueryable<Product> ordered = parameters.SafeSortBy switch
        {
            "createdAt" => parameters.IsSortDescending
                ? query.OrderByDescending(x => x.CreatedAt)
                : query.OrderBy(x => x.CreatedAt),
            "price" => parameters.IsSortDescending
                ? query.OrderByDescending(x => x.Price)
                : query.OrderBy(x => x.Price),
            "title" => parameters.IsSortDescending
                ? query.OrderByDescending(x => x.Title)
                : query.OrderBy(x => x.Title),
            _ => parameters.IsSortDescending
                ? query.OrderByDescending(x => x.UpdatedAt)
                : query.OrderBy(x => x.UpdatedAt),
        };

        var adminPageSlice = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var projected = await (
            from p in adminPageSlice
            join c in dbContext.Categories.AsNoTracking() on p.CategoryId equals c.Id into cg
            from category in cg.DefaultIfEmpty()
            select new
            {
                p.Id,
                p.Title,
                p.Slug,
                p.Price,
                p.Condition,
                p.Status,
                p.IsFeatured,
                p.FeaturedSortOrder,
                p.CreatedAt,
                p.UpdatedAt,
                p.ImageCount,
                p.CoverImageKey,
                CategoryName = category != null ? category.Name : null,
            })
            .ToListAsync(cancellationToken);

        var items = projected
            .Select(p => new AdminProductListItemDto(
                p.Id,
                p.Title,
                p.Slug,
                p.Price,
                p.Condition?.ToString(),
                p.Status.ToString(),
                p.CategoryName,
                p.ImageCount,
                p.CoverImageKey,
                p.IsFeatured,
                p.FeaturedSortOrder,
                p.CreatedAt,
                p.UpdatedAt))
            .ToList();

        return new PagedResult<AdminProductListItemDto>(items, page, pageSize, totalCount);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await dbContext.Products.AddAsync(product, cancellationToken);
    }

    private static List<Guid> CollectDescendantCategoryIds(
        Guid categoryId,
        IReadOnlyCollection<CategoryHierarchyNode> categories)
    {
        var childCategoriesByParentId = categories
            .Where(category => category.ParentId.HasValue)
            .GroupBy(category => category.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.Select(x => x.Id).ToList());

        var descendantCategoryIds = new List<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(categoryId);

        while (queue.Count > 0)
        {
            var currentCategoryId = queue.Dequeue();
            descendantCategoryIds.Add(currentCategoryId);

            if (!childCategoriesByParentId.TryGetValue(currentCategoryId, out var childCategoryIds))
            {
                continue;
            }

            foreach (var childCategoryId in childCategoryIds)
            {
                queue.Enqueue(childCategoryId);
            }
        }

        return descendantCategoryIds;
    }

    private sealed record CategoryHierarchyNode(Guid Id, string Slug, Guid? ParentId);
}
