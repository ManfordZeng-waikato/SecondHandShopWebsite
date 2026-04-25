using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using SecondHandShop.Application.Abstractions.ImageProcessing;
using SecondHandShop.Application.Abstractions.Messaging;
using SecondHandShop.Application.Abstractions.Security;
using SecondHandShop.Application.Abstractions.Storage;
using SecondHandShop.Infrastructure.Persistence;

namespace SecondHandShop.WebApi.IntegrationTests.RealStack;

/// <summary>
/// WebApplicationFactory wired to a real Postgres container (via <see cref="RealStackPostgresFixture"/>)
/// with real EF, MediatR, repositories, JwtTokenService and PasswordHasherService.
/// Only external SDKs (Turnstile, R2, remove.bg, SMTP) are replaced with deterministic fakes.
/// </summary>
public sealed class RealStackWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string AdminUserName = "real-stack-admin";
    public const string AdminPassword = "Real-Stack-Admin-Pa55!";

    public string ConnectionString { get; set; } = "";

    public FakeTurnstileValidator TurnstileValidator { get; } = new();
    public FakeObjectStorageService ObjectStorage { get; } = new();
    public FakeBackgroundRemovalService BackgroundRemoval { get; } = new();
    public RecordingEmailSender EmailSender { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["Cors:AllowedOrigins:0"] = "https://localhost:5173",
                ["Jwt:Key"] = "real-stack-tests-jwt-key-real-stack-tests-jwt-key",
                ["Jwt:Issuer"] = "SecondHandShop.RealStackTests",
                ["Jwt:Audience"] = "SecondHandShop.RealStackTests",
                ["Jwt:AccessTokenMinutes"] = "20",
                // Fixture already migrated; do NOT migrate or seed default catalog twice.
                ["Database:ApplyMigrationsOnStartup"] = "false",
                ["Database:SeedAdminOnStartup"] = "false",
                ["Database:SeedCatalogOnStartup"] = "false",
                ["Database:EnsureE2EAdminOnStartup"] = "true",
                ["E2EAdminSeed:UserName"] = AdminUserName,
                ["E2EAdminSeed:Password"] = AdminPassword,
                ["AdminAuth:Cookie:SameSite"] = "Strict",
                ["AdminAuth:Cookie:Secure"] = "true",
                ["AdminAuth:Cookie:Path"] = "/",
                ["Email:Smtp:Enabled"] = "false",
                ["R2:AccountId"] = "fake-account",
                ["R2:AccessKeyId"] = "fake-access-key",
                ["R2:SecretAccessKey"] = "fake-secret",
                ["R2:BucketName"] = "fake-bucket",
                ["R2:WorkerBaseUrl"] = "https://img.example.test",
                ["RemoveBg:ApiKey"] = "fake-remove-bg",
                ["CloudflareTurnstile:SecretKey"] = "fake-turnstile-secret",
                ["CloudflareTurnstile:VerifyUrl"] = "https://turnstile.example.test/siteverify",
                ["Security:RequireCloudflare"] = "false"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<SecondHandShopDbContext>>();
            services.RemoveAll<IDbContextFactory<SecondHandShopDbContext>>();
            services.AddDbContext<SecondHandShopDbContext>(options =>
                options.UseNpgsql(ConnectionString));
            services.AddDbContextFactory<SecondHandShopDbContext>(
                options => options.UseNpgsql(ConnectionString),
                ServiceLifetime.Scoped);

            services.RemoveAll<ITurnstileValidator>();
            services.AddSingleton<ITurnstileValidator>(TurnstileValidator);

            services.RemoveAll<IObjectStorageService>();
            services.AddSingleton<IObjectStorageService>(ObjectStorage);

            services.RemoveAll<IBackgroundRemovalService>();
            services.AddSingleton<IBackgroundRemovalService>(BackgroundRemoval);

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(EmailSender);
        });
    }
}

public sealed class FakeTurnstileValidator : ITurnstileValidator
{
    public bool ShouldSucceed { get; set; } = true;

    public Task<TurnstileValidationResult> ValidateAsync(
        string token,
        string? remoteIpAddress,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TurnstileValidationResult
        {
            IsSuccess = ShouldSucceed,
            ErrorCodes = ShouldSucceed ? Array.Empty<string>() : new[] { "fake-invalid-token" }
        });
    }
}

public sealed class FakeObjectStorageService : IObjectStorageService
{
    public Task<PresignedUploadUrlResult> CreatePresignedUploadUrlAsync(
        PresignedUploadUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new PresignedUploadUrlResult(
            $"https://fake-r2.example.test/upload/{request.ObjectKey}",
            DateTime.UtcNow.Add(request.ExpiresIn)));
    }

    public Task DeleteObjectAsync(string objectKey, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public string BuildDisplayUrl(string objectKey)
        => $"https://img.example.test/{objectKey}";
}

public sealed class FakeBackgroundRemovalService : IBackgroundRemovalService
{
    public Task<BackgroundRemovalResult> RemoveBackgroundAsync(
        Stream imageStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var buffer = new MemoryStream();
        imageStream.CopyTo(buffer);
        buffer.Position = 0;
        return Task.FromResult(new BackgroundRemovalResult(buffer, contentType));
    }
}

public sealed class RecordingEmailSender : IEmailSender
{
    public ConcurrentQueue<InquiryEmailMessage> InquiryMessages { get; } = new();
    public ConcurrentQueue<AdminLoginNotificationMessage> AdminLoginMessages { get; } = new();

    public Task SendInquiryAsync(InquiryEmailMessage message, CancellationToken cancellationToken = default)
    {
        InquiryMessages.Enqueue(message);
        return Task.CompletedTask;
    }

    public Task SendAdminLoginNotificationAsync(
        AdminLoginNotificationMessage message,
        CancellationToken cancellationToken = default)
    {
        AdminLoginMessages.Enqueue(message);
        return Task.CompletedTask;
    }
}
