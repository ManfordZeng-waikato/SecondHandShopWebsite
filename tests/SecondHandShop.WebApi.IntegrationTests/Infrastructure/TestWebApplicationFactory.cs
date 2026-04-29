using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using MediatR;
using Moq;
using SecondHandShop.Application.Abstractions.ImageProcessing;
using SecondHandShop.Application.Abstractions.Storage;
using SecondHandShop.Application.UseCases.Sales;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Security;
using SecondHandShop.Application.UseCases.Catalog;
using SecondHandShop.Application.UseCases.Customers;
using SecondHandShop.Application.UseCases.Inquiries;
using SecondHandShop.Application.UseCases.Analytics;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Infrastructure.Persistence;

namespace SecondHandShop.WebApi.IntegrationTests.Infrastructure;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string AdminCookieName = "shs.admin.token";

    public Guid ActiveAdminId { get; private set; }
    public Mock<IAdminCatalogService> AdminCatalogServiceMock { get; } = new();
    public Mock<IAdminCustomerService> AdminCustomerServiceMock { get; } = new();
    public Mock<IAdminSaleService> AdminSaleServiceMock { get; } = new();
    public Mock<IAnalyticsService> AnalyticsServiceMock { get; } = new();
    public Mock<ICategoryRepository> CategoryRepositoryMock { get; } = new();
    public Mock<ICustomerRepository> CustomerRepositoryMock { get; } = new();
    public Mock<IInquiryService> InquiryServiceMock { get; } = new();
    public Mock<IInquiryRepository> InquiryRepositoryMock { get; } = new();
    public Mock<IProductRepository> ProductRepositoryMock { get; } = new();
    public Mock<IProductImageRepository> ProductImageRepositoryMock { get; } = new();
    public Mock<IObjectStorageService> ObjectStorageServiceMock { get; } = new();
    public Mock<IBackgroundRemovalService> BackgroundRemovalServiceMock { get; } = new();
    public Mock<IMediator> MediatorMock { get; } = new();

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
                // Inherits "true" from appsettings.Development.json otherwise. Without
                // this override Program.cs runs EnsureE2EAdminUserAsync at startup, which
                // resolves the DbContext registered by AddInfrastructure - that closure
                // captured the user-secrets connection string (e.g. Supabase) before any
                // test override applied, so the seed silently writes the e2e.admin row
                // into the developer's real database on every test run.
                ["Database:EnsureE2EAdminOnStartup"] = "false",
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
            // Belt-and-braces: even though every repository below is mocked, AddInfrastructure
            // already registered SecondHandShopDbContext with a closure capturing the dev
            // connection string. Eject it and rebind to an unreachable sentinel so any
            // residual code path that accidentally resolves DbContext fails loudly instead
            // of writing to the developer's real database.
            services.RemoveAll<SecondHandShopDbContext>();
            services.RemoveAll<DbContextOptions<SecondHandShopDbContext>>();
            services.RemoveAll<IDbContextFactory<SecondHandShopDbContext>>();
            services.AddDbContext<SecondHandShopDbContext>(options =>
                options.UseNpgsql("Host=test-no-real-db.invalid;Database=ignored;Username=ignored;Password=ignored"));

            services.RemoveAll<IHostedService>();
            services.RemoveAll<IAdminUserRepository>();
            services.RemoveAll<IAdminCatalogService>();
            services.RemoveAll<IAdminCustomerService>();
            services.RemoveAll<IAdminSaleService>();
            services.RemoveAll<IAnalyticsService>();
            services.RemoveAll<ICategoryRepository>();
            services.RemoveAll<ICustomerRepository>();
            services.RemoveAll<IInquiryService>();
            services.RemoveAll<IInquiryRepository>();
            services.RemoveAll<IProductRepository>();
            services.RemoveAll<IProductImageRepository>();
            services.RemoveAll<IObjectStorageService>();
            services.RemoveAll<IBackgroundRemovalService>();
            services.RemoveAll<IMediator>();

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
            services.AddScoped<IAdminCatalogService>(_ => AdminCatalogServiceMock.Object);
            services.AddScoped<IAdminCustomerService>(_ => AdminCustomerServiceMock.Object);
            services.AddScoped<IAdminSaleService>(_ => AdminSaleServiceMock.Object);
            services.AddScoped<IAnalyticsService>(_ => AnalyticsServiceMock.Object);
            services.AddScoped<ICategoryRepository>(_ => CategoryRepositoryMock.Object);
            services.AddScoped<ICustomerRepository>(_ => CustomerRepositoryMock.Object);
            services.AddScoped<IInquiryService>(_ => InquiryServiceMock.Object);
            services.AddScoped<IInquiryRepository>(_ => InquiryRepositoryMock.Object);
            services.AddScoped<IProductRepository>(_ => ProductRepositoryMock.Object);
            services.AddScoped<IProductImageRepository>(_ => ProductImageRepositoryMock.Object);
            services.AddScoped<IObjectStorageService>(_ => ObjectStorageServiceMock.Object);
            services.AddScoped<IBackgroundRemovalService>(_ => BackgroundRemovalServiceMock.Object);
            services.AddScoped<IMediator>(_ => MediatorMock.Object);
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

    public void ResetAppMocks()
    {
        AdminCatalogServiceMock.Reset();
        AdminCustomerServiceMock.Reset();
        AdminSaleServiceMock.Reset();
        AnalyticsServiceMock.Reset();
        CategoryRepositoryMock.Reset();
        CustomerRepositoryMock.Reset();
        InquiryServiceMock.Reset();
        InquiryRepositoryMock.Reset();
        ProductRepositoryMock.Reset();
        ProductImageRepositoryMock.Reset();
        ObjectStorageServiceMock.Reset();
        BackgroundRemovalServiceMock.Reset();
        MediatorMock.Reset();
    }
}
