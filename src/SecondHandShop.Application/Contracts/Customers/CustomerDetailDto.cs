namespace SecondHandShop.Application.Contracts.Customers;

public sealed record CustomerDetailDto(
    Guid Id,
    string? Name,
    string? Email,
    string? Phone,
    string Status,
    string? Notes,
    int InquiryCount,
    DateTime? LastInquiryAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);
