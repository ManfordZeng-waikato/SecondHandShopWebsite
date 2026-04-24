using FluentAssertions;
using SecondHandShop.Domain.Common;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Domain.UnitTests.Entities;

public class ProductSaleTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Admin = Guid.NewGuid();

    [Fact]
    public void Create_ShouldInitialiseAsCompleted_AndTrimBuyerDetails()
    {
        var sale = ProductSale.Create(
            productId: Guid.NewGuid(),
            listedPriceAtSale: 200m,
            finalSoldPrice: 180m,
            soldAtUtc: UtcNow.AddMinutes(-30),
            adminUserId: Admin,
            utcNow: UtcNow,
            buyerName: "  Alice  ",
            buyerPhone: "  123  ",
            buyerEmail: "  alice@example.com  ",
            paymentMethod: PaymentMethod.Cash,
            notes: "  fast deal  ");

        sale.Status.Should().Be(SaleRecordStatus.Completed);
        sale.BuyerName.Should().Be("Alice");
        sale.BuyerPhone.Should().Be("123");
        sale.BuyerEmail.Should().Be("alice@example.com");
        sale.Notes.Should().Be("fast deal");
        sale.PaymentMethod.Should().Be(PaymentMethod.Cash);
        sale.CreatedByAdminUserId.Should().Be(Admin);
    }

    [Fact]
    public void Create_ShouldThrow_WhenFinalPriceIsNegative()
    {
        var act = () => ProductSale.Create(
            Guid.NewGuid(), 100m, finalSoldPrice: -1m, UtcNow, Admin, UtcNow);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Cancel_ShouldMarkCancelled_AndPreserveBusinessFields()
    {
        var sale = ProductSale.Create(
            Guid.NewGuid(), 200m, 180m, UtcNow.AddMinutes(-30), Admin, UtcNow,
            buyerEmail: "alice@example.com");
        var later = UtcNow.AddHours(1);

        sale.Cancel(SaleCancellationReason.BuyerBackedOut, "customer-asked", Admin, later);

        sale.Status.Should().Be(SaleRecordStatus.Cancelled);
        sale.CancelledAtUtc.Should().Be(later);
        sale.CancellationReason.Should().Be(SaleCancellationReason.BuyerBackedOut);
        sale.CancellationNote.Should().Be("customer-asked");
        sale.FinalSoldPrice.Should().Be(180m, "cancellation must never rewrite historical price");
        sale.BuyerEmail.Should().Be("alice@example.com");
        sale.UpdatedAt.Should().Be(later);
    }

    [Fact]
    public void Cancel_ShouldThrow_WhenAlreadyCancelled()
    {
        var sale = ProductSale.Create(Guid.NewGuid(), 100m, 100m, UtcNow, Admin, UtcNow);
        sale.Cancel(SaleCancellationReason.Other, null, Admin, UtcNow.AddMinutes(1));

        var act = () => sale.Cancel(SaleCancellationReason.Other, null, Admin, UtcNow.AddMinutes(2));

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("*already cancelled*");
    }

    [Fact]
    public void Cancel_ShouldConvertBlankNoteToNull()
    {
        var sale = ProductSale.Create(Guid.NewGuid(), 100m, 100m, UtcNow, Admin, UtcNow);

        sale.Cancel(SaleCancellationReason.Other, "   ", Admin, UtcNow.AddMinutes(1));

        sale.CancellationNote.Should().BeNull();
    }
}
