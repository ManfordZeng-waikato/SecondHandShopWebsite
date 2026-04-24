using FluentAssertions;
using SecondHandShop.Domain.Common;

namespace SecondHandShop.Domain.UnitTests.Common;

public class EmailAddressSyntaxValidatorTests
{
    [Theory]
    [InlineData("alice@example.com")]
    [InlineData("ALICE@EXAMPLE.COM")]
    [InlineData("alice.smith@mail.co.uk")]
    [InlineData("a+tag@example.io")]
    [InlineData("user.name+tag@sub.domain.example")]
    public void IsValid_ShouldAccept_WellFormedAddresses(string email)
    {
        EmailAddressSyntaxValidator.IsValid(email).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("plaintext")]
    [InlineData("@missing-local.com")]
    [InlineData("missing-at-sign.com")]
    [InlineData("double@@example.com")]
    [InlineData("trailing.dot@example.")]
    [InlineData("a@b.c", "single-char TLD is rejected per spec")]
    [InlineData("a@b.c1", "numeric TLD is rejected")]
    public void IsValid_ShouldReject_Malformed(string email, string _ = "")
    {
        EmailAddressSyntaxValidator.IsValid(email).Should().BeFalse();
    }

    [Fact]
    public void IsValid_ShouldReject_WhenLongerThan256Chars()
    {
        var local = new string('a', 250);
        var email = $"{local}@example.com";
        email.Length.Should().BeGreaterThan(256);

        EmailAddressSyntaxValidator.IsValid(email).Should().BeFalse();
    }
}
