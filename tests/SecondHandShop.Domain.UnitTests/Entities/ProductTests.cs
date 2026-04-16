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
