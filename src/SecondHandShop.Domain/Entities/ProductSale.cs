using SecondHandShop.Domain.Common;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Domain.Entities;

/// <summary>
/// Immutable record of a single sale event for a product.
/// Business fields (buyer, price, sold time) are set at creation and never changed.
/// A sale can only transition from <c>Completed</c> to <c>Cancelled</c>; it cannot be "uncancelled".
/// </summary>
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

    public SaleRecordStatus Status { get; private set; } = SaleRecordStatus.Completed;
    public DateTime? CancelledAtUtc { get; private set; }
    public SaleCancellationReason? CancellationReason { get; private set; }
    public string? CancellationNote { get; private set; }

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
            Notes = notes?.Trim(),
            Status = SaleRecordStatus.Completed
        };

        sale.SetCreatedAudit(adminUserId, utcNow);
        return sale;
    }

    /// <summary>
    /// Mark this sale record as cancelled. Business fields (buyer, price, sold time) remain
    /// untouched so the historical sale is preserved verbatim.
    /// </summary>
    public void Cancel(
        SaleCancellationReason reason,
        string? cancellationNote,
        Guid? adminUserId,
        DateTime utcNow)
    {
        if (Status == SaleRecordStatus.Cancelled)
        {
            throw new DomainRuleViolationException("Sale record is already cancelled.");
        }

        Status = SaleRecordStatus.Cancelled;
        CancelledAtUtc = utcNow;
        CancellationReason = reason;
        CancellationNote = string.IsNullOrWhiteSpace(cancellationNote)
            ? null
            : cancellationNote.Trim();
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
