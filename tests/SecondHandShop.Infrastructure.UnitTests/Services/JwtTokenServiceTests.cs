using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SecondHandShop.Application.Security;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Infrastructure.Services;

namespace SecondHandShop.Infrastructure.UnitTests.Services;

public class JwtTokenServiceTests
{
    private const string TestKey = "tests-only-jwt-key-tests-only-jwt-key";
    private const string Issuer = "SecondHandShop.Tests";
    private const string Audience = "SecondHandShop.Tests";

    [Fact]
    public void CreateToken_ShouldProduceTokenThatValidatesAgainstSameKey()
    {
        var service = CreateService();
        var admin = AdminUser.CreateWithCredentials("lord", "Lord", "hash");

        var (token, expiresAt) = service.CreateToken(admin);

        token.Should().NotBeNullOrWhiteSpace();
        expiresAt.Should().BeAfter(DateTimeOffset.UtcNow);

        var handler = new JwtSecurityTokenHandler();
        handler.ValidateToken(token, TokenValidationParameters(), out _);
        var jwt = handler.ReadJwtToken(token);
        jwt.Claims.Should().Contain(c =>
            c.Type == JwtRegisteredClaimNames.Sub && c.Value == admin.Id.ToString());
        jwt.Claims.Should().Contain(c =>
            (c.Type == "role" || c.Type == ClaimTypes.Role) && c.Value == "Admin");
        jwt.Claims.Should().Contain(c =>
            c.Type == AdminJwtClaimTypes.TokenVersion && c.Value == "0");
        jwt.Claims.Should().NotContain(c => c.Type == AdminJwtClaimTypes.PasswordChangeRequired);
    }

    [Fact]
    public void CreateToken_ShouldStampPasswordChangeRequiredClaim_WhenAdminRequiresIt()
    {
        var service = CreateService();
        var admin = AdminUser.CreateWithCredentials("lord", "Lord", "hash", mustChangePassword: true);

        var (token, _) = service.CreateToken(admin);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        jwt.Claims.Should().Contain(c =>
            c.Type == AdminJwtClaimTypes.PasswordChangeRequired && c.Value == "true");
    }

    [Fact]
    public void CreateToken_ShouldEmbedCurrentTokenVersion()
    {
        var service = CreateService();
        var admin = AdminUser.CreateWithCredentials("lord", "Lord", "hash");
        admin.BumpTokenVersion();
        admin.BumpTokenVersion();

        var (token, _) = service.CreateToken(admin);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        jwt.Claims.Should().Contain(c =>
            c.Type == AdminJwtClaimTypes.TokenVersion && c.Value == "2");
    }

    [Fact]
    public void CreateToken_ExpiryShouldHonourAccessTokenMinutesSetting()
    {
        var service = CreateService(accessTokenMinutes: "7");
        var admin = AdminUser.CreateWithCredentials("lord", "Lord", "hash");
        var before = DateTimeOffset.UtcNow;

        var (_, expiresAt) = service.CreateToken(admin);

        expiresAt.Should().BeCloseTo(before.AddMinutes(7), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void TokenSignedWithDifferentKey_ShouldFailValidation()
    {
        var service = CreateService();
        var admin = AdminUser.CreateWithCredentials("lord", "Lord", "hash");
        var (token, _) = service.CreateToken(admin);

        var handler = new JwtSecurityTokenHandler();
        var wrongKey = TokenValidationParameters();
        wrongKey.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            "different-key-different-key-different-key"));

        var act = () => handler.ValidateToken(token, wrongKey, out _);

        act.Should().Throw<SecurityTokenInvalidSignatureException>();
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenJwtKeyMissing()
    {
        var config = BuildConfig(key: null);

        var act = () => new JwtTokenService(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Key*");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenJwtKeyTooShort()
    {
        var config = BuildConfig(key: "too-short");

        var act = () => new JwtTokenService(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least 32*");
    }

    [Theory]
    [InlineData("4")]
    [InlineData("1441")]
    public void Constructor_ShouldThrow_WhenAccessTokenMinutesOutOfRange(string minutes)
    {
        var config = BuildConfig(accessTokenMinutes: minutes);

        var act = () => new JwtTokenService(config);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*between 5 and 1440*");
    }

    private static JwtTokenService CreateService(string accessTokenMinutes = "20") =>
        new(BuildConfig(accessTokenMinutes: accessTokenMinutes));

    private static IConfiguration BuildConfig(
        string? key = TestKey,
        string accessTokenMinutes = "20")
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = Issuer,
            ["Jwt:Audience"] = Audience,
            ["Jwt:AccessTokenMinutes"] = accessTokenMinutes
        };
        if (key is not null)
            values["Jwt:Key"] = key;
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static TokenValidationParameters TokenValidationParameters() => new()
    {
        ValidIssuer = Issuer,
        ValidAudience = Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey)),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = false,
        ClockSkew = TimeSpan.FromSeconds(5)
    };
}
