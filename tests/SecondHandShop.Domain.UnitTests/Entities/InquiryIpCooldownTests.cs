using FluentAssertions;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Domain.UnitTests.Entities;

public class InquiryIpCooldownTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldTrimIp_AndStampFields()
    {
        var cooldown = InquiryIpCooldown.Create("  203.0.113.1  ", UtcNow.AddHours(1), UtcNow);

        cooldown.IpAddress.Should().Be("203.0.113.1");
        cooldown.BlockedUntil.Should().Be(UtcNow.AddHours(1));
        cooldown.UpdatedAt.Should().Be(UtcNow);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenIpIsBlank(string ip)
    {
        var act = () => InquiryIpCooldown.Create(ip, UtcNow, UtcNow);

        act.Should().Throw<ArgumentException>()
            .And.ParamName.Should().Be("ipAddress");
    }

    [Fact]
    public void SetCooldown_ShouldExtendBlockUntil_AndUpdateTimestamp()
    {
        var cooldown = InquiryIpCooldown.Create("203.0.113.1", UtcNow.AddMinutes(5), UtcNow);
        var later = UtcNow.AddMinutes(4);
        var newBlockUntil = UtcNow.AddHours(2);

        cooldown.SetCooldown(newBlockUntil, later);

        cooldown.BlockedUntil.Should().Be(newBlockUntil);
        cooldown.UpdatedAt.Should().Be(later);
    }
}
