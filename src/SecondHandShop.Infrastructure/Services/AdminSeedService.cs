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

        if (await dbContext.AdminUsers.AnyAsync())
        {
            logger.LogInformation("Admin users already exist, skipping seed.");
            return;
        }

        var userName = configuration["AdminSeed:UserName"];
        var password = configuration["AdminSeed:Password"];

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("AdminSeed:UserName or AdminSeed:Password is not configured. Skipping admin seed.");
            return;
        }

        var hash = passwordHasher.Hash(password);
        var admin = AdminUser.CreateWithCredentials(userName, userName, hash, "Admin");
        await dbContext.AdminUsers.AddAsync(admin);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Seeded admin user '{UserName}'.", userName);
    }
}
