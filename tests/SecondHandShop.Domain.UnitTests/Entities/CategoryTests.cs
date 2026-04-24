using FluentAssertions;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Domain.UnitTests.Entities;

public class CategoryTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Admin = Guid.NewGuid();

    [Fact]
    public void Create_ShouldTrimName_LowercaseSlug_AndStampAudit()
    {
        var category = Category.Create(
            name: "  Vintage Bags  ",
            slug: "Vintage-Bags",
            parentId: null,
            sortOrder: 1,
            isActive: true,
            createdByAdminUserId: Admin,
            utcNow: UtcNow);

        category.Name.Should().Be("Vintage Bags");
        category.Slug.Should().Be("vintage-bags");
        category.IsActive.Should().BeTrue();
        category.CreatedAt.Should().Be(UtcNow);
        category.CreatedByAdminUserId.Should().Be(Admin);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenNameIsBlank(string name)
    {
        var act = () => Category.Create(name, "slug", null, 0, true, Admin, UtcNow);

        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("name");
    }

    [Fact]
    public void Create_ShouldThrow_WhenNameExceedsMaxLength()
    {
        var longName = new string('x', Category.NameMaxLength + 1);

        var act = () => Category.Create(longName, "slug", null, 0, true, Admin, UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("UPPER CASE WITH SPACES")]
    [InlineData("bad_slug_underscore")]
    [InlineData("中文-slug")]
    public void Create_ShouldThrow_WhenSlugIsInvalid(string slug)
    {
        var act = () => Category.Create("Name", slug, null, 0, true, Admin, UtcNow);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_ShouldRenameAndReslug_AndTouchAudit()
    {
        var category = Category.Create("Old", "old", null, 0, true, Admin, UtcNow);
        var laterAdmin = Guid.NewGuid();
        var later = UtcNow.AddMinutes(10);

        category.Update("New Name", "new-name", null, 2, isActive: false, laterAdmin, later);

        category.Name.Should().Be("New Name");
        category.Slug.Should().Be("new-name");
        category.SortOrder.Should().Be(2);
        category.IsActive.Should().BeFalse();
        category.UpdatedAt.Should().Be(later);
        category.UpdatedByAdminUserId.Should().Be(laterAdmin);
    }
}
