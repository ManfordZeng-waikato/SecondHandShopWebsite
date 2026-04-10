namespace SecondHandShop.Application.Contracts.Sales;

/// <summary>
/// Input to mark a product as sold. Creates a new <c>ProductSale</c> row; never updates
/// an existing one. Only valid when the product is currently Available or OffShelf.
/// </summary>
public sealed record MarkProductSoldRequest(
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
