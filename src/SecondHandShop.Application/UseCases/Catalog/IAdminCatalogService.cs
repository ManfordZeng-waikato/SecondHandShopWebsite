using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Application.UseCases.Catalog;

public interface IAdminCatalogService
{
    Task<Guid> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task UpdateProductStatusAsync(Guid productId, ProductStatus status, Guid? adminUserId, CancellationToken cancellationToken = default);
    Task<CreateProductImageUploadUrlResponse> CreateProductImageUploadUrlAsync(
        CreateProductImageUploadUrlRequest request,
        CancellationToken cancellationToken = default);
    Task AddProductImageAsync(AddProductImageRequest request, CancellationToken cancellationToken = default);
}

public sealed record CreateProductRequest(
    string Title,
    string Slug,
    string Description,
    decimal Price,
    ProductCondition Condition,
    Guid CategoryId,
    Guid? AdminUserId);

public sealed record CreateProductImageUploadUrlRequest(
    Guid ProductId,
    string FileName,
    string ContentType,
    Guid? AdminUserId);

public sealed record CreateProductImageUploadUrlResponse(
    string UploadUrl,
    string ObjectKey,
    string PublicUrl,
    DateTime ExpiresAtUtc);

public sealed record AddProductImageRequest(
    Guid ProductId,
    string ObjectKey,
    string Url,
    string? AltText,
    int SortOrder,
    bool IsPrimary,
    Guid? AdminUserId);
