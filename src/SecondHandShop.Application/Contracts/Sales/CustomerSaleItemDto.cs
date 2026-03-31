namespace SecondHandShop.Application.Contracts.Sales;

public sealed record CustomerSaleItemDto(
    Guid SaleId,
    Guid ProductId,
    string ProductTitle,
    string? ProductSlug,
    decimal FinalSoldPrice,
    DateTime SoldAtUtc,
    string? PaymentMethod,
    Guid? InquiryId);
