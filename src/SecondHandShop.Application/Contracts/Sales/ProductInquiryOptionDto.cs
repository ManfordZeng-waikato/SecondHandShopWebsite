namespace SecondHandShop.Application.Contracts.Sales;

public sealed record ProductInquiryOptionDto(
    Guid InquiryId,
    string? CustomerName,
    string? Email,
    string? PhoneNumber,
    string Message,
    DateTime CreatedAt,
    Guid? LinkedSaleId);
