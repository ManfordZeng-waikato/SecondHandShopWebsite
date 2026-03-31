using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Infrastructure.Persistence;

namespace SecondHandShop.Infrastructure.Services;

public static class CatalogSeedService
{
    public static async Task SeedDefaultCategoriesIfEmptyAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SecondHandShopDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(CatalogSeedService));

        if (await dbContext.Categories.AnyAsync())
        {
            logger.LogInformation("Categories already exist, skipping catalog seed.");
            return;
        }

        var utcNow = DateTime.UtcNow;
        var createdBy = await dbContext.AdminUsers.Select(x => (Guid?)x.Id).FirstOrDefaultAsync();

        var categories = new[]
        {
            Category.Create("Electronics", "electronics", null, 1, createdBy, utcNow),
            Category.Create("Furniture", "furniture", null, 2, createdBy, utcNow),
            Category.Create("Home Appliances", "home-appliances", null, 3, createdBy, utcNow),
        };

        await dbContext.Categories.AddRangeAsync(categories);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} default categories.", categories.Length);
    }
}
