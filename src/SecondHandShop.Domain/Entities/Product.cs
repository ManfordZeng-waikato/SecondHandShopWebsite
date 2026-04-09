using SecondHandShop.Domain.Common;
using static SecondHandShop.Domain.Common.SlugValidator;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Domain.Entities;

public class Product : AuditableEntity
{
    public const int FeaturedSortOrderMin = 0;
    public const int FeaturedSortOrderMax = 999;

    private Product()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public ProductCondition? Condition { get; private set; }
    public ProductStatus Status { get; private set; } = ProductStatus.Available;
    public Guid CategoryId { get; private set; }
    public DateTime? SoldAt { get; private set; }
    public DateTime? OffShelvedAt { get; private set; }
    public bool IsFeatured { get; private set; }
    public int? FeaturedSortOrder { get; private set; }

    /// <summary>
    /// Denormalized from product images: primary first, then ascending sort order (same as list queries).
    /// </summary>
    public string? CoverImageKey { get; private set; }

    /// <summary>
    /// Denormalized count of images for this product.
    /// </summary>
    public int ImageCount { get; private set; }

    public static Product Create(
        string title,
        string slug,
        string description,
        decimal price,
        Guid categoryId,
        Guid? createdByAdminUserId,
        DateTime utcNow,
        ProductCondition? condition = null)
    {
        ValidatePrice(price);

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Product title is required.", nameof(title));
        }

        SlugValidator.EnsureValid(slug, nameof(slug));

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Description = description?.Trim() ?? string.Empty,
            Price = price,
            Condition = condition,
            CategoryId = categoryId,
            Status = ProductStatus.Available
        };

        product.CoverImageKey = null;
        product.ImageCount = 0;
        product.SetCreatedAudit(createdByAdminUserId, utcNow);
        return product;
    }

    /// <summary>
    /// Keeps cover key and image count in sync with the current image set after add/remove/reorder operations.
    /// </summary>
    public void SyncImageDenormalization(string? coverImageKey, int imageCount, Guid? updatedByAdminUserId, DateTime utcNow)
    {
        CoverImageKey = coverImageKey;
        ImageCount = imageCount;
        Touch(updatedByAdminUserId, utcNow);
    }

    public void UpdateDetails(
        string title,
        string slug,
        string description,
        decimal price,
        Guid categoryId,
        Guid? updatedByAdminUserId,
        DateTime utcNow,
        ProductCondition? condition = null)
    {
        ValidatePrice(price);

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Product title is required.", nameof(title));
        }

        SlugValidator.EnsureValid(slug, nameof(slug));

        Title = title.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = description?.Trim() ?? string.Empty;
        Price = price;
        Condition = condition;
        CategoryId = categoryId;
        Touch(updatedByAdminUserId, utcNow);
    }

    public void MarkAsSold(Guid? updatedByAdminUserId, DateTime utcNow)
    {
        Status = ProductStatus.Sold;
        SoldAt = utcNow;
        OffShelvedAt = null;
        IsFeatured = false;
        FeaturedSortOrder = null;
        Touch(updatedByAdminUserId, utcNow);
    }

    public void OffShelf(Guid? updatedByAdminUserId, DateTime utcNow)
    {
        Status = ProductStatus.OffShelf;
        OffShelvedAt = utcNow;
        IsFeatured = false;
        FeaturedSortOrder = null;
        Touch(updatedByAdminUserId, utcNow);
    }

    public void MarkAsAvailable(Guid? updatedByAdminUserId, DateTime utcNow)
    {
        Status = ProductStatus.Available;
        SoldAt = null;
        OffShelvedAt = null;
        Touch(updatedByAdminUserId, utcNow);
    }

    public void UpdateFeaturedSettings(
        bool isFeatured,
        int? featuredSortOrder,
        Guid? updatedByAdminUserId,
        DateTime utcNow)
    {
        if (isFeatured && Status != ProductStatus.Available)
        {
            throw new DomainRuleViolationException("Only available products can be featured.");
        }

        if (featuredSortOrder.HasValue &&
            (featuredSortOrder.Value < FeaturedSortOrderMin || featuredSortOrder.Value > FeaturedSortOrderMax))
        {
            throw new ArgumentOutOfRangeException(nameof(featuredSortOrder), BuildFeaturedSortOrderOutOfRangeMessage());
        }

        var normalizedSortOrder = isFeatured ? featuredSortOrder : null;
        if (IsFeatured == isFeatured && FeaturedSortOrder == normalizedSortOrder)
        {
            return;
        }

        IsFeatured = isFeatured;
        FeaturedSortOrder = normalizedSortOrder;
        Touch(updatedByAdminUserId, utcNow);
    }

    public static string BuildFeaturedSortOrderOutOfRangeMessage()
    {
        return $"Featured sort order must be between {FeaturedSortOrderMin} and {FeaturedSortOrderMax}. " +
               $"Smaller values appear earlier.";
    }

    private static void ValidatePrice(decimal price)
    {
        if (price <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Product price must be greater than zero.");
        }
    }
}
