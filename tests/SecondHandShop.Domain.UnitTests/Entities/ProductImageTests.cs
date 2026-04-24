using FluentAssertions;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Domain.UnitTests.Entities;

public class ProductImageTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Admin = Guid.NewGuid();

    [Fact]
    public void Create_ShouldTrimKey_NormaliseAlt_AndClearLegacyUrl()
    {
        var image = ProductImage.Create(
            productId: Guid.NewGuid(),
            cloudStorageKey: "  products/abc.jpg  ",
            altText: "  Blue bag  ",
            sortOrder: 3,
            isPrimary: true,
            createdByAdminUserId: Admin,
            utcNow: UtcNow);

        image.CloudStorageKey.Should().Be("products/abc.jpg");
        image.AltText.Should().Be("Blue bag");
        image.SortOrder.Should().Be(3);
        image.IsPrimary.Should().BeTrue();
        image.Url.Should().BeEmpty("legacy URL column is not populated for new records");
        image.CreatedAt.Should().Be(UtcNow);
        image.CreatedByAdminUserId.Should().Be(Admin);
    }

    [Fact]
    public void Create_ShouldConvertBlankAltToNull()
    {
        var image = ProductImage.Create(
            Guid.NewGuid(), "k", altText: "   ", sortOrder: 0, isPrimary: false,
            createdByAdminUserId: Admin, utcNow: UtcNow);

        image.AltText.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenKeyIsBlank(string key)
    {
        var act = () => ProductImage.Create(Guid.NewGuid(), key, null, 0, false, Admin, UtcNow);

        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("cloudStorageKey");
    }

    [Fact]
    public void Update_ShouldOverwriteFields_AndTouchAudit()
    {
        var image = ProductImage.Create(Guid.NewGuid(), "k1", "a1", 0, isPrimary: false, Admin, UtcNow);
        var laterAdmin = Guid.NewGuid();
        var later = UtcNow.AddMinutes(5);

        image.Update("k2", "a2", 7, isPrimary: true, laterAdmin, later);

        image.CloudStorageKey.Should().Be("k2");
        image.AltText.Should().Be("a2");
        image.SortOrder.Should().Be(7);
        image.IsPrimary.Should().BeTrue();
        image.UpdatedAt.Should().Be(later);
        image.UpdatedByAdminUserId.Should().Be(laterAdmin);
    }
}
