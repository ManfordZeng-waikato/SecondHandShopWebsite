using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Moq;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Security;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.WebApi.IntegrationTests.Infrastructure;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string AdminCookieName = "shs.admin.token";

    public Guid ActiveAdminId { get; private set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://localhost:5173",
                ["Jwt:Key"] = "tests-only-jwt-key-tests-only-jwt-key",
                ["Jwt:Issuer"] = "SecondHandShop.Tests",
                ["Jwt:Audience"] = "SecondHandShop.Tests",
                ["Jwt:AccessTokenMinutes"] = "20",
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=ignored;Username=ignored;Password=ignored",
                ["Database:ApplyMigrationsOnStartup"] = "false",
                ["Database:SeedAdminOnStartup"] = "false",
                ["Database:SeedCatalogOnStartup"] = "false",
                ["AdminAuth:Cookie:SameSite"] = "Strict",
                ["AdminAuth:Cookie:Secure"] = "true",
                ["AdminAuth:Cookie:Path"] = "/api/lord",
                ["Email:Smtp:Enabled"] = "false",
                ["R2:AccountId"] = "test-account",
                ["R2:AccessKeyId"] = "test-access-key",
                ["R2:SecretAccessKey"] = "test-secret",
                ["R2:BucketName"] = "test-bucket",
                ["R2:WorkerBaseUrl"] = "https://img.example.test",
                ["RemoveBg:ApiKey"] = "test-remove-bg",
                ["CloudflareTurnstile:SecretKey"] = "test-turnstile-secret",
                ["CloudflareTurnstile:VerifyUrl"] = "https://turnstile.example.test/siteverify"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();
            services.RemoveAll<IAdminUserRepository>();

            var activeAdmin = AdminUser.CreateWithCredentials("lord", "Lord", "hashed-password");
            ActiveAdminId = activeAdmin.Id;

            var repository = new Mock<IAdminUserRepository>();
            repository
                .Setup(x => x.GetByIdAsync(ActiveAdminId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(activeAdmin);
            repository
                .Setup(x => x.GetByUserNameAsync(activeAdmin.UserName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(activeAdmin);
            repository
                .Setup(x => x.AnyAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            services.AddScoped<IAdminUserRepository>(_ => repository.Object);
        });
    }

    public string CreateAdminToken(bool requiresPasswordChange = false, int tokenVersion = 0)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, ActiveAdminId.ToString()),
            new(ClaimTypes.NameIdentifier, ActiveAdminId.ToString()),
            new(ClaimTypes.Role, "Admin"),
            new(AdminJwtClaimTypes.TokenVersion, tokenVersion.ToString()),
        };

        if (requiresPasswordChange)
        {
            claims.Add(new(AdminJwtClaimTypes.PasswordChangeRequired, "true"));
        }

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes("tests-only-jwt-key-tests-only-jwt-key")),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "SecondHandShop.Tests",
            audience: "SecondHandShop.Tests",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(20),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string CreateCookieHeader(string jwt)
        => $"{AdminCookieName}={jwt}";
}
