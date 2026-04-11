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
    public Category Category { get; private set; } = null!;
    public ICollection<ProductCategory> ProductCategories { get; private set; } = new List<ProductCategory>();

    /// <summary>
    /// Pointer to the currently active <see cref="ProductSale"/>. Non-null iff
    /// <see cref="Status"/> == <see cref="ProductStatus.Sold"/>. History lives on
    /// <c>ProductSales</c> (one row per sale attempt, never mutated after creation).
    /// </summary>
    public Guid? CurrentSaleId { get; private set; }

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

    public void UpdateMainCategory(Guid categoryId, Guid? updatedByAdminUserId, DateTime utcNow)
    {
        if (CategoryId == categoryId)
        {
            return;
        }

        CategoryId = categoryId;
        Touch(updatedByAdminUserId, utcNow);
    }

    /// <summary>
    /// Transitions the product to Sold and returns a newly created <see cref="ProductSale"/>
    /// history record. Only valid from Available or OffShelf — reverting a Sold product must
    /// go through <see cref="RevertSoldToAvailable"/> first so the previous sale is preserved
    /// as a cancelled history row.
    /// </summary>
    public ProductSale MarkAsSold(
        decimal finalSoldPrice,
        DateTime soldAtUtc,
        Guid? adminUserId,
        DateTime utcNow,
        Guid? customerId = null,
        Guid? inquiryId = null,
        string? buyerName = null,
        string? buyerPhone = null,
        string? buyerEmail = null,
        PaymentMethod? paymentMethod = null,
        string? notes = null)
    {
        if (Status == ProductStatus.Sold)
        {
            throw new DomainRuleViolationException(
                "Product is already sold. Revert the current sale before marking it sold again.");
        }

        var sale = ProductSale.Create(
            productId: Id,
            listedPriceAtSale: Price,
            finalSoldPrice: finalSoldPrice,
            soldAtUtc: soldAtUtc,
            adminUserId: adminUserId,
            utcNow: utcNow,
            customerId: customerId,
            inquiryId: inquiryId,
            buyerName: buyerName,
            buyerPhone: buyerPhone,
            buyerEmail: buyerEmail,
            paymentMethod: paymentMethod,
            notes: notes);

        Status = ProductStatus.Sold;
        SoldAt = soldAtUtc;
        OffShelvedAt = null;
        IsFeatured = false;
        FeaturedSortOrder = null;
        CurrentSaleId = sale.Id;
        Touch(adminUserId, utcNow);
        return sale;
    }

    /// <summary>
    /// Reverts a sold product back to Available. Marks the current sale as Cancelled with a
    /// reason — history is preserved, nothing is deleted or overwritten.
    /// </summary>
    public void RevertSoldToAvailable(
        ProductSale currentSale,
        SaleCancellationReason reason,
        string? cancellationNote,
        Guid? adminUserId,
        DateTime utcNow)
    {
        if (Status != ProductStatus.Sold)
        {
            throw new DomainRuleViolationException("Only sold products can be reverted to available.");
        }

        if (CurrentSaleId is null || currentSale.Id != CurrentSaleId || currentSale.ProductId != Id)
        {
            throw new DomainRuleViolationException(
                "The provided sale record does not match the product's current sale.");
        }

        currentSale.Cancel(reason, cancellationNote, adminUserId, utcNow);

        Status = ProductStatus.Available;
        SoldAt = null;
        CurrentSaleId = null;
        Touch(adminUserId, utcNow);
    }

    public void OffShelf(Guid? updatedByAdminUserId, DateTime utcNow)
    {
        if (Status == ProductStatus.Sold)
        {
            throw new DomainRuleViolationException(
                "Cannot take a sold product off the shelf. Revert the sale first.");
        }

        Status = ProductStatus.OffShelf;
        OffShelvedAt = utcNow;
        IsFeatured = false;
        FeaturedSortOrder = null;
        Touch(updatedByAdminUserId, utcNow);
    }

    /// <summary>
    /// Restore an off-shelf product back to the available pool. This path is NOT used for the
    /// Sold→Available transition — that goes through <see cref="RevertSoldToAvailable"/> so a
    /// cancellation reason is captured.
    /// </summary>
    public void RestoreFromOffShelf(Guid? updatedByAdminUserId, DateTime utcNow)
    {
        if (Status != ProductStatus.OffShelf)
        {
            throw new DomainRuleViolationException(
                "Only off-shelf products can be restored via this path. " +
                "Use RevertSoldToAvailable for sold products.");
        }

        Status = ProductStatus.Available;
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
