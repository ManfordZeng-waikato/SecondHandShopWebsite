using FluentAssertions;
using SecondHandShop.Domain.Common;

namespace SecondHandShop.Domain.UnitTests.Common;

public class SlugValidatorTests
{
    [Theory]
    [InlineData("a")]
    [InlineData("vintage-leather-bag")]
    [InlineData("a1-b2-c3")]
    [InlineData("item123")]
    public void IsValid_ShouldAccept_CanonicalSlugs(string slug)
    {
        SlugValidator.IsValid(slug).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("UPPERCASE")]
    [InlineData("with spaces")]
    [InlineData("trailing-")]
    [InlineData("-leading")]
    [InlineData("double--hyphen")]
    [InlineData("under_score")]
    [InlineData("中文")]
    [InlineData("emoji-🙂")]
    public void IsValid_ShouldReject_InvalidInputs(string slug)
    {
        SlugValidator.IsValid(slug).Should().BeFalse();
    }

    [Fact]
    public void EnsureValid_ShouldThrow_WithParamName_WhenBlank()
    {
        var act = () => SlugValidator.EnsureValid("   ", "categorySlug");

        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("categorySlug");
    }

    [Fact]
    public void EnsureValid_ShouldThrow_WhenFormatIsInvalid()
    {
        var act = () => SlugValidator.EnsureValid("NOT valid", "slug");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*lowercase*");
    }

    [Fact]
    public void EnsureValid_ShouldAcceptMixedCaseInput_AfterNormalisation()
    {
        var act = () => SlugValidator.EnsureValid("Vintage-Leather-Bag", "slug");

        act.Should().NotThrow("EnsureValid trims + lower-cases before matching the regex");
    }
}
