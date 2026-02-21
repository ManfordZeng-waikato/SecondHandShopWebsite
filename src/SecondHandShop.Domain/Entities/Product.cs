using SecondHandShop.Domain.Common;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Domain.Entities;

public class Product : AuditableEntity
{
    private Product()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Title { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public ProductCondition Condition { get; private set; }
    public ProductStatus Status { get; private set; } = ProductStatus.Available;
    public Guid CategoryId { get; private set; }
    public DateTime? SoldAt { get; private set; }
    public DateTime? OffShelvedAt { get; private set; }

    public static Product Create(
        string title,
        string slug,
        string description,
        decimal price,
        ProductCondition condition,
        Guid categoryId,
        Guid? createdByAdminUserId,
        DateTime utcNow)
    {
        ValidatePrice(price);

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Product title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Product slug is required.", nameof(slug));
        }

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

        product.SetCreatedAudit(createdByAdminUserId, utcNow);
        return product;
    }

    public void UpdateDetails(
        string title,
        string slug,
        string description,
        decimal price,
        ProductCondition condition,
        Guid categoryId,
        Guid? updatedByAdminUserId,
        DateTime utcNow)
    {
        ValidatePrice(price);

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Product title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Product slug is required.", nameof(slug));
        }

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
        Touch(updatedByAdminUserId, utcNow);
    }

    public void OffShelf(Guid? updatedByAdminUserId, DateTime utcNow)
    {
        Status = ProductStatus.OffShelf;
        OffShelvedAt = utcNow;
        Touch(updatedByAdminUserId, utcNow);
    }

    public void MarkAsAvailable(Guid? updatedByAdminUserId, DateTime utcNow)
    {
        Status = ProductStatus.Available;
        SoldAt = null;
        OffShelvedAt = null;
        Touch(updatedByAdminUserId, utcNow);
    }

    private static void ValidatePrice(decimal price)
    {
        if (price <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Product price must be greater than zero.");
        }
    }
}
