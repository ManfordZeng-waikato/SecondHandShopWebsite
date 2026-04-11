using SecondHandShop.Application.Contracts.Catalog;
using SecondHandShop.Application.Contracts.Common;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Application.Abstractions.Persistence;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdWithCategoriesAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Product?> GetPublicBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Product?> GetPublicByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<ProductListItemDto>> ListPagedForPublicAsync(
        ProductQueryParameters parameters,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductListItemDto>> ListFeaturedForPublicAsync(
        int limit,
        CancellationToken cancellationToken = default);
    Task<PagedResult<AdminProductListItemDto>> ListPagedForAdminAsync(
        AdminProductQueryParameters parameters,
        CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
}
