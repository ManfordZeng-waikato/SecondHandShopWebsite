using FluentAssertions;
using SecondHandShop.Infrastructure.Services;

namespace SecondHandShop.Infrastructure.UnitTests.Services;

public class PasswordHasherServiceTests
{
    private readonly PasswordHasherService _sut = new();

    [Fact]
    public void Hash_ShouldProduceBCryptFormattedDigest()
    {
        var hash = _sut.Hash("correct-horse-battery-staple");

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().StartWith("$2", "BCrypt hashes begin with a $2 variant prefix");
        hash.Should().NotContain("correct-horse",
            "the raw password must never be retrievable from the digest");
    }

    [Fact]
    public void Hash_ShouldBeSaltedSoTwoHashesOfTheSamePasswordDiffer()
    {
        var h1 = _sut.Hash("password");
        var h2 = _sut.Hash("password");

        h1.Should().NotBe(h2, "each call must use a fresh random salt");
    }

    [Fact]
    public void Verify_ShouldReturnTrue_ForMatchingPassword()
    {
        var hash = _sut.Hash("my-password");

        _sut.Verify(hash, "my-password").Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldReturnFalse_WhenPasswordDoesNotMatch()
    {
        var hash = _sut.Hash("my-password");

        _sut.Verify(hash, "wrong-password").Should().BeFalse();
    }

    [Fact]
    public void Verify_ShouldBeCaseSensitive()
    {
        var hash = _sut.Hash("Secret");

        _sut.Verify(hash, "secret").Should().BeFalse();
    }
}
