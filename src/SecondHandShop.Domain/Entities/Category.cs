using SecondHandShop.Domain.Common;
using static SecondHandShop.Domain.Common.SlugValidator;

namespace SecondHandShop.Domain.Entities;

public class Category : AuditableEntity
{
    public const int NameMaxLength = 100;

    private Category()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public Guid? ParentId { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Category? Parent { get; private set; }
    public ICollection<Category> Children { get; private set; } = new List<Category>();
    public ICollection<ProductCategory> ProductCategories { get; private set; } = new List<ProductCategory>();

    public static Category Create(
        string name,
        string slug,
        Guid? parentId,
        int sortOrder,
        bool isActive,
        Guid? createdByAdminUserId,
        DateTime utcNow)
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = NormalizeName(name),
            Slug = NormalizeSlug(slug),
            ParentId = parentId,
            SortOrder = sortOrder,
            IsActive = isActive
        };

        category.SetCreatedAudit(createdByAdminUserId, utcNow);
        return category;
    }

    public void Update(
        string name,
        string slug,
        Guid? parentId,
        int sortOrder,
        bool isActive,
        Guid? updatedByAdminUserId,
        DateTime utcNow)
    {
        Name = NormalizeName(name);
        Slug = NormalizeSlug(slug);
        ParentId = parentId;
        SortOrder = sortOrder;
        IsActive = isActive;
        Touch(updatedByAdminUserId, utcNow);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Category name is required.", nameof(name));
        }

        var normalizedName = name.Trim();
        if (normalizedName.Length > NameMaxLength)
        {
            throw new ArgumentException(
                $"Category name cannot exceed {NameMaxLength} characters.",
                nameof(name));
        }

        return normalizedName;
    }

    private static string NormalizeSlug(string slug)
    {
        SlugValidator.EnsureValid(slug, nameof(slug));
        return slug.Trim().ToLowerInvariant();
    }
}
