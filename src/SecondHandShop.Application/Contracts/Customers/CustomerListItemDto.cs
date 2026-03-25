namespace SecondHandShop.Application.Contracts.Customers;

public sealed record CustomerListItemDto(
    Guid Id,
    string? Name,
    string? Email,
    string? Phone,
    string Status,
    int InquiryCount,
    DateTime? LastInquiryAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);
