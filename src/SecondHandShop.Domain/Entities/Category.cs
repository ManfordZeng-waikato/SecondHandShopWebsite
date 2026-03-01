using SecondHandShop.Domain.Common;
using static SecondHandShop.Domain.Common.SlugValidator;

namespace SecondHandShop.Domain.Entities;

public class Category : AuditableEntity
{
    private Category()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public Guid? ParentCategoryId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static Category Create(
        string name,
        string slug,
        Guid? parentCategoryId,
        int sortOrder,
        Guid? createdByAdminUserId,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name is required.", nameof(name));
        }

        SlugValidator.EnsureValid(slug, nameof(slug));

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            ParentCategoryId = parentCategoryId,
            SortOrder = sortOrder,
            IsActive = true
        };

        category.SetCreatedAudit(createdByAdminUserId, utcNow);
        return category;
    }

    public void Update(
        string name,
        string slug,
        Guid? parentCategoryId,
        int sortOrder,
        bool isActive,
        Guid? updatedByAdminUserId,
        DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name is required.", nameof(name));
        }

        SlugValidator.EnsureValid(slug, nameof(slug));

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        ParentCategoryId = parentCategoryId;
        SortOrder = sortOrder;
        IsActive = isActive;
        Touch(updatedByAdminUserId, utcNow);
    }
}
