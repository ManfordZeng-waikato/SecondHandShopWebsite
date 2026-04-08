namespace SecondHandShop.Application.Contracts.Customers;

public sealed record CustomerListItemDto(
    Guid Id,
    string? Name,
    string? Email,
    string? Phone,
    string Status,
    string PrimarySource,
    string ListedPrimarySource,
    int InquiryCount,
    DateTime? LastInquiryAt,
    int PurchaseCount,
    decimal TotalSpent,
    DateTime? LastPurchaseAtUtc,
    DateTime? LastContactAtUtc,
    DateTime CreatedAt,
    DateTime UpdatedAt);
