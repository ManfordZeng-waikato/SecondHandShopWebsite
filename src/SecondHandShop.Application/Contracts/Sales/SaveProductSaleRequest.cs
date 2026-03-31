namespace SecondHandShop.Application.Contracts.Sales;

public sealed record SaveProductSaleRequest(
    Guid ProductId,
    decimal FinalSoldPrice,
    DateTime SoldAtUtc,
    Guid? AdminUserId,
    Guid? CustomerId = null,
    Guid? InquiryId = null,
    string? BuyerName = null,
    string? BuyerPhone = null,
    string? BuyerEmail = null,
    string? PaymentMethod = null,
    string? Notes = null);
