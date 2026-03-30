namespace SecondHandShop.Application.Contracts.Catalog;

public sealed record AdminProductListItemDto(
    Guid Id,
    string Title,
    string Slug,
    decimal Price,
    string? Condition,
    string Status,
    string? CategoryName,
    int ImageCount,
    string? CoverImageKey,
    bool IsFeatured,
    int? FeaturedSortOrder,
    DateTime CreatedAt,
    DateTime UpdatedAt);
