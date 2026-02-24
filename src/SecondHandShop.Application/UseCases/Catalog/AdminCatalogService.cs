using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Abstractions.Storage;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Application.UseCases.Catalog;

public class AdminCatalogService(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IProductImageRepository productImageRepository,
    IObjectStorageService objectStorageService,
    IUnitOfWork unitOfWork,
    IClock clock) : IAdminCatalogService
{
    private const int MaxImagesPerProduct = 5;
    private static readonly HashSet<string> AllowedImageContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    public async Task<Guid> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var existingProduct = await productRepository.GetBySlugAsync(request.Slug.Trim().ToLowerInvariant(), cancellationToken);
        if (existingProduct is not null)
        {
            throw new InvalidOperationException($"Product slug '{request.Slug}' already exists.");
        }

        var category = await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null || !category.IsActive)
        {
            throw new KeyNotFoundException($"Category '{request.CategoryId}' was not found or inactive.");
        }

        var product = Product.Create(
            request.Title,
            request.Slug,
            request.Description,
            request.Price,
            request.Condition,
            request.CategoryId,
            request.AdminUserId,
            clock.UtcNow);

        await productRepository.AddAsync(product, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return product.Id;
    }

    public async Task UpdateProductStatusAsync(
        Guid productId,
        ProductStatus status,
        Guid? adminUserId,
        CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(productId, cancellationToken)
            ?? throw new KeyNotFoundException($"Product '{productId}' was not found.");

        switch (status)
        {
            case ProductStatus.Available:
                product.MarkAsAvailable(adminUserId, clock.UtcNow);
                break;
            case ProductStatus.Sold:
                product.MarkAsSold(adminUserId, clock.UtcNow);
                break;
            case ProductStatus.OffShelf:
                product.OffShelf(adminUserId, clock.UtcNow);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported product status.");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<CreateProductImageUploadUrlResponse> CreateProductImageUploadUrlAsync(
        CreateProductImageUploadUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = await productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new KeyNotFoundException($"Product '{request.ProductId}' was not found.");

        if (string.IsNullOrWhiteSpace(request.FileName))
        {
            throw new ArgumentException("FileName is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.ContentType))
        {
            throw new ArgumentException("ContentType is required.", nameof(request));
        }

        if (!AllowedImageContentTypes.Contains(request.ContentType.Trim()))
        {
            throw new InvalidOperationException("Only JPEG, PNG and WEBP images are allowed.");
        }

        var objectKey = BuildObjectKey(request.ProductId, request.FileName);
        var uploadResult = await objectStorageService.CreatePresignedUploadUrlAsync(
            new PresignedUploadUrlRequest(objectKey, request.ContentType.Trim(), TimeSpan.FromMinutes(10)),
            cancellationToken);
        var publicUrl = objectStorageService.BuildPublicUrl(objectKey);

        return new CreateProductImageUploadUrlResponse(
            uploadResult.UploadUrl,
            objectKey,
            publicUrl,
            uploadResult.ExpiresAtUtc);
    }

    public async Task AddProductImageAsync(AddProductImageRequest request, CancellationToken cancellationToken = default)
    {
        _ = await productRepository.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new KeyNotFoundException($"Product '{request.ProductId}' was not found.");

        if (string.IsNullOrWhiteSpace(request.ObjectKey))
        {
            throw new ArgumentException("ObjectKey is required.", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Url))
        {
            throw new ArgumentException("Url is required.", nameof(request));
        }

        var expectedKeyPrefix = $"products/{request.ProductId:N}/";
        if (!request.ObjectKey.StartsWith(expectedKeyPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("ObjectKey does not belong to the target product.");
        }

        var expectedUrl = objectStorageService.BuildPublicUrl(request.ObjectKey);
        if (!string.Equals(request.Url.Trim(), expectedUrl, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Image Url does not match ObjectKey public url.");
        }

        if (request.SortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "SortOrder must be zero or greater.");
        }

        var images = await productImageRepository.ListByProductIdAsync(request.ProductId, cancellationToken);
        if (images.Count >= MaxImagesPerProduct)
        {
            throw new InvalidOperationException($"A product can have at most {MaxImagesPerProduct} images.");
        }

        if (request.IsPrimary && images.Any(x => x.IsPrimary))
        {
            throw new InvalidOperationException("Product already has a primary image.");
        }

        var image = ProductImage.Create(
            request.ProductId,
            request.ObjectKey,
            request.Url,
            request.AltText,
            request.SortOrder,
            request.IsPrimary,
            request.AdminUserId,
            clock.UtcNow);

        await productImageRepository.AddAsync(image, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string BuildObjectKey(Guid productId, string fileName)
    {
        var normalizedFileName = Path.GetFileName(fileName).Trim();
        var extension = Path.GetExtension(normalizedFileName);
        var safeExtension = string.IsNullOrWhiteSpace(extension) ? ".bin" : extension.ToLowerInvariant();
        return $"products/{productId:N}/{Guid.NewGuid():N}{safeExtension}";
    }
}
