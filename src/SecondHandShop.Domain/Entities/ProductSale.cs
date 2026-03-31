using SecondHandShop.Domain.Common;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Domain.Entities;

public class ProductSale : AuditableEntity
{
    private ProductSale()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProductId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? InquiryId { get; private set; }
    public decimal ListedPriceAtSale { get; private set; }
    public decimal FinalSoldPrice { get; private set; }
    public string? BuyerName { get; private set; }
    public string? BuyerPhone { get; private set; }
    public string? BuyerEmail { get; private set; }
    public DateTime SoldAtUtc { get; private set; }
    public PaymentMethod? PaymentMethod { get; private set; }
    public string? Notes { get; private set; }

    public static ProductSale Create(
        Guid productId,
        decimal listedPriceAtSale,
        decimal finalSoldPrice,
        DateTime soldAtUtc,
        Guid? adminUserId,
        DateTime utcNow,
        Guid? customerId = null,
        Guid? inquiryId = null,
        string? buyerName = null,
        string? buyerPhone = null,
        string? buyerEmail = null,
        PaymentMethod? paymentMethod = null,
        string? notes = null)
    {
        ValidateFinalSoldPrice(finalSoldPrice);

        var sale = new ProductSale
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            CustomerId = customerId,
            InquiryId = inquiryId,
            ListedPriceAtSale = listedPriceAtSale,
            FinalSoldPrice = finalSoldPrice,
            BuyerName = buyerName?.Trim(),
            BuyerPhone = buyerPhone?.Trim(),
            BuyerEmail = buyerEmail?.Trim(),
            SoldAtUtc = soldAtUtc,
            PaymentMethod = paymentMethod,
            Notes = notes?.Trim()
        };

        sale.SetCreatedAudit(adminUserId, utcNow);
        return sale;
    }

    public void Update(
        decimal finalSoldPrice,
        DateTime soldAtUtc,
        Guid? adminUserId,
        DateTime utcNow,
        Guid? customerId = null,
        Guid? inquiryId = null,
        string? buyerName = null,
        string? buyerPhone = null,
        string? buyerEmail = null,
        PaymentMethod? paymentMethod = null,
        string? notes = null)
    {
        ValidateFinalSoldPrice(finalSoldPrice);

        FinalSoldPrice = finalSoldPrice;
        SoldAtUtc = soldAtUtc;
        CustomerId = customerId;
        InquiryId = inquiryId;
        BuyerName = buyerName?.Trim();
        BuyerPhone = buyerPhone?.Trim();
        BuyerEmail = buyerEmail?.Trim();
        PaymentMethod = paymentMethod;
        Notes = notes?.Trim();
        Touch(adminUserId, utcNow);
    }

    private static void ValidateFinalSoldPrice(decimal price)
    {
        if (price < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Final sold price cannot be negative.");
        }
    }
}
