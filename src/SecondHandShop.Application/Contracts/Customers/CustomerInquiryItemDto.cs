namespace SecondHandShop.Application.Contracts.Customers;

public sealed record CustomerInquiryItemDto(
    Guid InquiryId,
    Guid ProductId,
    string? ProductTitle,
    string? ProductSlug,
    string Message,
    string InquiryStatus,
    DateTime CreatedAt);
