namespace SecondHandShop.Application.Contracts.Catalog;

public sealed record ProductListItemDto(
    Guid Id,
    string Title,
    string Slug,
    decimal Price,
    string? CoverImageKey,
    string? CategoryName,
    string Status,
    string Condition,
    DateTime CreatedAt);
