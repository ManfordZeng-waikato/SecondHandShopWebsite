namespace SecondHandShop.Application.Contracts.Customers;

public sealed record CustomerDetailDto(
    Guid Id,
    string? Name,
    string? Email,
    string? Phone,
    string Status,
    string PrimarySource,
    string? Notes,
    int InquiryCount,
    DateTime? LastInquiryAt,
    int PurchaseCount,
    decimal TotalSpent,
    DateTime? LastPurchaseAtUtc,
    DateTime? LastContactAtUtc,
    DateTime CreatedAt,
    DateTime UpdatedAt);
