using FluentAssertions;
using SecondHandShop.Domain.Common;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Domain.UnitTests.Entities;

public class ProductTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 16, 1, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void MarkAsSold_ShouldCreateHistoryRecord_AndClearFeaturedState()
    {
        var product = CreateProduct();
        product.UpdateFeaturedSettings(true, 3, updatedByAdminUserId: null, utcNow: UtcNow);

        var sale = product.MarkAsSold(
            finalSoldPrice: 180m,
            soldAtUtc: UtcNow.AddMinutes(-30),
            adminUserId: Guid.NewGuid(),
            utcNow: UtcNow,
            buyerEmail: "buyer@example.com");

        sale.ProductId.Should().Be(product.Id);
        product.Status.Should().Be(ProductStatus.Sold);
        product.CurrentSaleId.Should().Be(sale.Id);
        product.SoldAt.Should().Be(UtcNow.AddMinutes(-30));
        product.IsFeatured.Should().BeFalse();
        product.FeaturedSortOrder.Should().BeNull();
    }

    [Fact]
    public void RevertSoldToAvailable_ShouldCancelCurrentSale_AndRestoreAvailability()
    {
        var product = CreateProduct();
        var sale = product.MarkAsSold(
            finalSoldPrice: 180m,
            soldAtUtc: UtcNow.AddMinutes(-30),
            adminUserId: Guid.NewGuid(),
            utcNow: UtcNow,
            buyerPhone: "021 555 000");

        product.RevertSoldToAvailable(
            sale,
            SaleCancellationReason.AdminMistake,
            "Captured wrong buyer",
            adminUserId: Guid.NewGuid(),
            utcNow: UtcNow.AddMinutes(5));

        product.Status.Should().Be(ProductStatus.Available);
        product.CurrentSaleId.Should().BeNull();
        product.SoldAt.Should().BeNull();
        sale.Status.Should().Be(SaleRecordStatus.Cancelled);
        sale.CancellationReason.Should().Be(SaleCancellationReason.AdminMistake);
    }

    [Fact]
    public void OffShelf_ShouldRejectSoldProducts()
    {
        var product = CreateProduct();
        _ = product.MarkAsSold(
            finalSoldPrice: 180m,
            soldAtUtc: UtcNow.AddMinutes(-30),
            adminUserId: Guid.NewGuid(),
            utcNow: UtcNow);

        var act = () => product.OffShelf(updatedByAdminUserId: null, utcNow: UtcNow);

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("Cannot take a sold product off the shelf*");
    }

    [Fact]
    public void UpdatePrice_ShouldSetNewPriceAndTouchAudit_WhenAvailable()
    {
        var product = CreateProduct();
        var admin = Guid.NewGuid();
        var later = UtcNow.AddMinutes(5);

        product.UpdatePrice(259m, admin, later);

        product.Price.Should().Be(259m);
        product.UpdatedAt.Should().Be(later);
        product.UpdatedByAdminUserId.Should().Be(admin);
    }

    [Fact]
    public void UpdatePrice_ShouldAllowUpdate_WhenOffShelf()
    {
        var product = CreateProduct();
        product.OffShelf(Guid.NewGuid(), UtcNow.AddMinutes(1));

        product.UpdatePrice(199m, Guid.NewGuid(), UtcNow.AddMinutes(2));

        product.Price.Should().Be(199m);
    }

    [Fact]
    public void UpdatePrice_ShouldThrow_WhenSold()
    {
        var product = CreateProduct();
        _ = product.MarkAsSold(200m, UtcNow.AddMinutes(1), Guid.NewGuid(), UtcNow.AddMinutes(1));

        var act = () => product.UpdatePrice(180m, Guid.NewGuid(), UtcNow.AddMinutes(2));

        act.Should().Throw<DomainRuleViolationException>()
            .WithMessage("Cannot modify price of a sold product. Revert the sale first.");
    }

    [Fact]
    public void UpdatePrice_ShouldThrow_WhenNonPositive()
    {
        var product = CreateProduct();

        var act = () => product.UpdatePrice(0m, Guid.NewGuid(), UtcNow.AddMinutes(1));

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void UpdatePrice_ShouldBeNoop_WhenPriceUnchanged()
    {
        var product = CreateProduct();
        var originalUpdatedAt = product.UpdatedAt;

        product.UpdatePrice(product.Price, Guid.NewGuid(), UtcNow.AddMinutes(10));

        product.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    private static Product CreateProduct()
    {
        return Product.Create(
            title: "Vintage leather bag",
            slug: "vintage-leather-bag",
            description: "Soft grain leather bag.",
            price: 220m,
            categoryId: Guid.NewGuid(),
            createdByAdminUserId: Guid.NewGuid(),
            utcNow: UtcNow,
            condition: ProductCondition.Good);
    }
}
