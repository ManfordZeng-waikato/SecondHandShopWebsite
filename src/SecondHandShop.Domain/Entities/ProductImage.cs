using SecondHandShop.Domain.Common;

namespace SecondHandShop.Domain.Entities;

public class ProductImage : AuditableEntity
{
    private ProductImage()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProductId { get; private set; }
    public string CloudStorageKey { get; private set; } = string.Empty;

    /// <summary>
    /// Legacy column kept for backward compatibility during migration.
    /// New records store empty string. Display URLs are computed at runtime via Worker.
    /// </summary>
    public string Url { get; private set; } = string.Empty;

    public string? AltText { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsPrimary { get; private set; }

    public static ProductImage Create(
        Guid productId,
        string cloudStorageKey,
        string? altText,
        int sortOrder,
        bool isPrimary,
        Guid? createdByAdminUserId,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(cloudStorageKey))
        {
            throw new ArgumentException("Cloud storage key is required.", nameof(cloudStorageKey));
        }

        var image = new ProductImage
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            CloudStorageKey = cloudStorageKey.Trim(),
            Url = string.Empty,
            AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim(),
            SortOrder = sortOrder,
            IsPrimary = isPrimary
        };

        image.SetCreatedAudit(createdByAdminUserId, utcNow);
        return image;
    }

    public void Update(
        string cloudStorageKey,
        string? altText,
        int sortOrder,
        bool isPrimary,
        Guid? updatedByAdminUserId,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(cloudStorageKey))
        {
            throw new ArgumentException("Cloud storage key is required.", nameof(cloudStorageKey));
        }

        CloudStorageKey = cloudStorageKey.Trim();
        Url = string.Empty;
        AltText = string.IsNullOrWhiteSpace(altText) ? null : altText.Trim();
        SortOrder = sortOrder;
        IsPrimary = isPrimary;
        Touch(updatedByAdminUserId, utcNow);
    }
}
