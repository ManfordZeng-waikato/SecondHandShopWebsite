using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SecondHandShop.Infrastructure.Persistence;

namespace SecondHandShop.Infrastructure.UnitTests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_ShouldReadDbConnectionStringWhenDbContextIsResolved()
    {
        const string initialConnectionString =
            "Host=initial-db.example.test;Database=initial;Username=test;Password=test";
        const string finalConnectionString =
            "Host=final-db.example.test;Database=final;Username=test;Password=test";

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = initialConnectionString,
                ["Email:Smtp:Enabled"] = "false",
                ["R2:AccountId"] = "test-account",
                ["R2:AccessKeyId"] = "test-access-key",
                ["R2:SecretAccessKey"] = "test-secret",
                ["R2:BucketName"] = "test-bucket",
                ["R2:WorkerBaseUrl"] = "https://img.example.test",
                ["RemoveBg:ApiKey"] = "test-remove-bg",
                ["CloudflareTurnstile:SecretKey"] = "test-turnstile-secret",
                ["CloudflareTurnstile:VerifyUrl"] = "https://turnstile.example.test/siteverify"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddInfrastructure(configuration);

        configuration["ConnectionStrings:DefaultConnection"] = finalConnectionString;

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<SecondHandShopDbContext>();
        var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<SecondHandShopDbContext>>();
        using var factoryDbContext = dbContextFactory.CreateDbContext();

        dbContext.Database.GetConnectionString().Should().Be(finalConnectionString);
        factoryDbContext.Database.GetConnectionString().Should().Be(finalConnectionString);
    }
}
