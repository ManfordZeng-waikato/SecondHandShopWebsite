using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SecondHandShop.Application.Abstractions.Security;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Infrastructure.Persistence;

namespace SecondHandShop.Infrastructure.Services;

public static class AdminSeedService
{
    public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SecondHandShopDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecondHandShopDbContext>>();

        var userName = configuration["AdminSeed:UserName"];
        var password = configuration["AdminSeed:Password"];

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("AdminSeed:UserName or AdminSeed:Password is not configured. Skipping admin seed.");
            return;
        }

        var normalizedUserName = userName.Trim();
        var e2eUserName = configuration["E2EAdminSeed:UserName"]?.Trim();
        var seedUserExists = await dbContext.AdminUsers.AnyAsync(x => x.UserName == normalizedUserName);
        var nonAutomationAdminExists = string.IsNullOrWhiteSpace(e2eUserName)
            ? await dbContext.AdminUsers.AnyAsync()
            : await dbContext.AdminUsers.AnyAsync(x => x.UserName != e2eUserName);

        if (seedUserExists || nonAutomationAdminExists)
        {
            logger.LogInformation("Admin users already exist, skipping seed.");
            return;
        }

        var hash = passwordHasher.Hash(password);
        var admin = AdminUser.CreateWithCredentials(normalizedUserName, normalizedUserName, hash, "Admin", mustChangePassword: true);
        await dbContext.AdminUsers.AddAsync(admin);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Seeded admin user '{UserName}'.", normalizedUserName);
    }

    public static async Task EnsureE2EAdminUserAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SecondHandShopDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecondHandShopDbContext>>();

        var userName = configuration["E2EAdminSeed:UserName"];
        var password = configuration["E2EAdminSeed:Password"];

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("E2EAdminSeed:UserName or E2EAdminSeed:Password is not configured. Skipping E2E admin seed.");
            return;
        }

        var normalizedUserName = userName.Trim();
        var hash = passwordHasher.Hash(password);
        var existing = await dbContext.AdminUsers
            .FirstOrDefaultAsync(x => x.UserName == normalizedUserName);

        if (existing is null)
        {
            var admin = AdminUser.CreateWithCredentials(
                normalizedUserName,
                "Playwright E2E Admin",
                hash,
                "Admin",
                mustChangePassword: false);
            await dbContext.AdminUsers.AddAsync(admin);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Created E2E admin user '{UserName}'.", normalizedUserName);
            return;
        }

        existing.ResetCredentialsForBootstrap(hash, mustChangePassword: false);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Reset E2E admin user '{UserName}' credentials for local automation.", normalizedUserName);
    }
}
