using FluentAssertions;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Domain.UnitTests.Entities;

public class AdminUserTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateWithCredentials_ShouldInitialiseSafeDefaults()
    {
        var admin = AdminUser.CreateWithCredentials("lord", "Lord", "hash");

        admin.UserName.Should().Be("lord");
        admin.DisplayName.Should().Be("Lord");
        admin.PasswordHash.Should().Be("hash");
        admin.Role.Should().Be("Admin");
        admin.IsActive.Should().BeTrue();
        admin.MustChangePassword.Should().BeFalse();
        admin.TokenVersion.Should().Be(0);
        admin.FailedLoginCount.Should().Be(0);
        admin.LockedUntilUtc.Should().BeNull();
    }

    [Theory]
    [InlineData("", "Lord", "hash")]
    [InlineData("  ", "Lord", "hash")]
    [InlineData("lord", "", "hash")]
    [InlineData("lord", "Lord", "")]
    public void CreateWithCredentials_ShouldThrow_WhenRequiredFieldIsMissing(
        string userName, string displayName, string passwordHash)
    {
        var act = () => AdminUser.CreateWithCredentials(userName, displayName, passwordHash);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CompleteForcedPasswordChange_ShouldClearFlag_AndBumpTokenVersion()
    {
        var admin = AdminUser.CreateWithCredentials("lord", "Lord", "old-hash", mustChangePassword: true);

        admin.CompleteForcedPasswordChange("new-hash");

        admin.PasswordHash.Should().Be("new-hash");
        admin.MustChangePassword.Should().BeFalse();
        admin.TokenVersion.Should().Be(1,
            "bumping the version invalidates the transient JWT used to call change-password");
    }

    [Fact]
    public void CompleteForcedPasswordChange_ShouldThrow_WhenHashIsBlank()
    {
        var admin = AdminUser.CreateWithCredentials("lord", "Lord", "hash");

        var act = () => admin.CompleteForcedPasswordChange("   ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RegisterFailedLogin_ShouldLockAccount_OnceAttemptsReachLimit()
    {
        var admin = AdminUser.CreateWithCredentials("lord", "Lord", "hash");

        for (var i = 0; i < 4; i++)
            admin.RegisterFailedLogin(maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15), utcNow: UtcNow);

        admin.LockedUntilUtc.Should().BeNull("the 4 failures are below the threshold");

        admin.RegisterFailedLogin(maxAttempts: 5, lockoutDuration: TimeSpan.FromMinutes(15), utcNow: UtcNow);

        admin.LockedUntilUtc.Should().Be(UtcNow.AddMinutes(15));
        admin.IsLockedOut(UtcNow.AddMinutes(1)).Should().BeTrue();
        admin.IsLockedOut(UtcNow.AddMinutes(16)).Should().BeFalse();
    }

    [Fact]
    public void RegisterSuccessfulLogin_ShouldResetFailures_AndStoreIp()
    {
        var admin = AdminUser.CreateWithCredentials("lord", "Lord", "hash");
        admin.RegisterFailedLogin(5, TimeSpan.FromMinutes(15), UtcNow);

        admin.RegisterSuccessfulLogin(UtcNow, "203.0.113.10");

        admin.FailedLoginCount.Should().Be(0);
        admin.LockedUntilUtc.Should().BeNull();
        admin.LastSuccessfulLoginAtUtc.Should().Be(UtcNow);
        admin.LastSuccessfulLoginIp.Should().Be("203.0.113.10");
    }

    [Fact]
    public void SetActive_ShouldBumpTokenVersion_OnlyWhenDeactivating()
    {
        var admin = AdminUser.CreateWithCredentials("lord", "Lord", "hash");

        admin.SetActive(true);
        admin.TokenVersion.Should().Be(0, "re-activating an already active account is a no-op for token version");

        admin.SetActive(false);
        admin.TokenVersion.Should().Be(1, "deactivation invalidates outstanding sessions");

        admin.SetActive(false);
        admin.TokenVersion.Should().Be(1, "deactivating an already inactive account does not bump again");
    }

    [Fact]
    public void ResetCredentialsForBootstrap_ShouldRestoreActiveState_AndBumpTokenVersion()
    {
        var admin = AdminUser.CreateWithCredentials("lord", "Lord", "hash");
        admin.RegisterFailedLogin(5, TimeSpan.FromMinutes(15), UtcNow);
        admin.RegisterFailedLogin(5, TimeSpan.FromMinutes(15), UtcNow);
        admin.RegisterFailedLogin(5, TimeSpan.FromMinutes(15), UtcNow);
        admin.RegisterFailedLogin(5, TimeSpan.FromMinutes(15), UtcNow);
        admin.RegisterFailedLogin(5, TimeSpan.FromMinutes(15), UtcNow);
        admin.SetActive(false);
        var versionBeforeReset = admin.TokenVersion;

        admin.ResetCredentialsForBootstrap("reset-hash", mustChangePassword: true);

        admin.PasswordHash.Should().Be("reset-hash");
        admin.MustChangePassword.Should().BeTrue();
        admin.IsActive.Should().BeTrue();
        admin.FailedLoginCount.Should().Be(0);
        admin.LockedUntilUtc.Should().BeNull();
        admin.TokenVersion.Should().BeGreaterThan(versionBeforeReset);
    }
}
