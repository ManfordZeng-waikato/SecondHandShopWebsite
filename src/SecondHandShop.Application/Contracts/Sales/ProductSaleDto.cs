namespace SecondHandShop.Application.Contracts.Sales;

public sealed record ProductSaleDto(
    Guid Id,
    Guid ProductId,
    Guid? CustomerId,
    Guid? InquiryId,
    decimal ListedPriceAtSale,
    decimal FinalSoldPrice,
    string? BuyerName,
    string? BuyerPhone,
    string? BuyerEmail,
    DateTime SoldAtUtc,
    string? PaymentMethod,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt);
