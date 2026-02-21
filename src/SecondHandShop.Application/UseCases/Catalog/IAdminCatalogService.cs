using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Application.UseCases.Catalog;

public interface IAdminCatalogService
{
    Task<Guid> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task UpdateProductStatusAsync(Guid productId, ProductStatus status, Guid? adminUserId, CancellationToken cancellationToken = default);
}

public sealed record CreateProductRequest(
    string Title,
    string Slug,
    string Description,
    decimal Price,
    ProductCondition Condition,
    Guid CategoryId,
    Guid? AdminUserId);
