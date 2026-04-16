using Microsoft.EntityFrameworkCore;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Contracts.Catalog;
using SecondHandShop.Application.Contracts.Common;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Infrastructure.Persistence.Repositories;

public class ProductRepository(
    SecondHandShopDbContext dbContext,
    ICategoryHierarchyCache categoryHierarchyCache) : IProductRepository
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
        var query =
            from p in dbContext.Products.AsNoTracking()
            join c in dbContext.Categories.AsNoTracking().Where(c => c.IsActive) on p.CategoryId equals c.Id
            where p.Status == ProductStatus.Available || p.Status == ProductStatus.Sold
            select new
            {
                Product = p,
                CategoryName = c.Name
            };

        var safeSearch = parameters.SafeSearch;
        if (safeSearch is not null)
        {
            var pattern = $"%{EscapeLikePattern(safeSearch)}%";
            query = query.Where(x => EF.Functions.ILike(x.Product.Title, pattern, "\\"));
        }

        var safeCategory = parameters.SafeCategory;
        if (safeCategory is not null)
        {
            var categorySlug = safeCategory.ToLowerInvariant();
            var snapshot = await categoryHierarchyCache.GetAsync(cancellationToken);
            var selectedCategory = snapshot.FindBySlug(categorySlug);

            if (selectedCategory is null)
            {
                query = query.Where(_ => false);
            }
            else
            {
                var descendantCategoryIds = snapshot.GetDescendantIds(selectedCategory.Id);
                query = query.Where(x =>
                    dbContext.ProductCategories.Any(pc =>
                        pc.ProductId == x.Product.Id &&
                        descendantCategoryIds.Contains(pc.CategoryId)));
            }
        }

        var safeCategoryIds = parameters.SafeCategoryIds;
        if (safeCategoryIds.Count > 0)
        {
            query = query.Where(x =>
                dbContext.ProductCategories.Any(pc =>
                    pc.ProductId == x.Product.Id &&
                    safeCategoryIds.Contains(pc.CategoryId)));
        }

        if (parameters.MinPrice.HasValue)
            query = query.Where(x => x.Product.Price >= parameters.MinPrice.Value);

        if (parameters.MaxPrice.HasValue)
            query = query.Where(x => x.Product.Price <= parameters.MaxPrice.Value);

        if (!string.IsNullOrWhiteSpace(parameters.Status)
            && Enum.TryParse<ProductStatus>(parameters.Status, ignoreCase: true, out var statusEnum)
            && statusEnum is ProductStatus.Available or ProductStatus.Sold)
        {
            query = query.Where(x => x.Product.Status == statusEnum);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var page = parameters.SafePage;
        var pageSize = parameters.SafePageSize;

        var orderedQuery = parameters.Sort switch
        {
            "price_asc" => query.OrderBy(x => x.Product.Price).ThenByDescending(x => x.Product.CreatedAt),
            "price_desc" => query.OrderByDescending(x => x.Product.Price).ThenByDescending(x => x.Product.CreatedAt),
            _ => query.OrderByDescending(x => x.Product.CreatedAt),
        };

        var pageSlice = orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var projected = await pageSlice
            .Select(x => new
            {
                x.Product.Id,
                x.Product.Title,
                x.Product.Slug,
                x.Product.Price,
                x.Product.Status,
                x.Product.Condition,
                x.Product.CreatedAt,
                x.Product.CoverImageKey,
                x.CategoryName
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

        var query =
            from p in dbContext.Products.AsNoTracking()
            join c in dbContext.Categories.AsNoTracking().Where(c => c.IsActive) on p.CategoryId equals c.Id
            where p.IsFeatured
            where p.Status == ProductStatus.Available
            select new
            {
                Product = p,
                CategoryName = c.Name
            };

        var limitedQuery = query
            .OrderBy(x => x.Product.FeaturedSortOrder.HasValue ? 0 : 1)
            .ThenBy(x => x.Product.FeaturedSortOrder)
            .ThenByDescending(x => x.Product.CreatedAt)
            .ThenBy(x => x.Product.Id)
            .Take(safeLimit);

        var projected = await limitedQuery
            .Select(x => new
            {
                x.Product.Id,
                x.Product.Title,
                x.Product.Slug,
                x.Product.Price,
                x.Product.Status,
                x.Product.Condition,
                x.Product.CreatedAt,
                x.Product.CoverImageKey,
                x.CategoryName
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
        {
            var pattern = $"%{EscapeLikePattern(search)}%";
            query = query.Where(x => EF.Functions.ILike(x.Title, pattern, "\\"));
        }

        if (!string.IsNullOrWhiteSpace(parameters.Status)
            && Enum.TryParse<ProductStatus>(parameters.Status, ignoreCase: true, out var statusEnum))
        {
            query = query.Where(x => x.Status == statusEnum);
        }

        if (parameters.CategoryId.HasValue)
        {
            var snapshot = await categoryHierarchyCache.GetAsync(cancellationToken);
            var selectedCategoryId = parameters.CategoryId.Value;
            if (!snapshot.ById.ContainsKey(selectedCategoryId))
            {
                query = query.Where(_ => false);
            }
            else
            {
                var descendantCategoryIds = snapshot.GetDescendantIds(selectedCategoryId);
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

    public async Task<IReadOnlyDictionary<Guid, ProductEmailInfoDto>> GetEmailInfoByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return new Dictionary<Guid, ProductEmailInfoDto>();

        return await dbContext.Products
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new ProductEmailInfoDto(p.Id, p.Title, p.Slug))
            .ToDictionaryAsync(p => p.Id, cancellationToken);
    }

    private static string EscapeLikePattern(string value)
        => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
