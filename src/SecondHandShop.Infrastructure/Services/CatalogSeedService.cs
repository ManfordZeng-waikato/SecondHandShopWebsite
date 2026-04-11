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
        var createdBy = await dbContext.AdminUsers
            .OrderBy(x => x.Id)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync();

        var categories = new[]
        {
            Category.Create("Furniture", "furniture", null, 1, true, createdBy, utcNow),
            Category.Create("Antique", "antique", null, 2, true, createdBy, utcNow),
            Category.Create("Outdoor", "outdoor", null, 3, true, createdBy, utcNow),
        };

        await dbContext.Categories.AddRangeAsync(categories);
        await dbContext.SaveChangesAsync();

        var furniture = categories.Single(x => x.Slug == "furniture");
        var childCategories = new[]
        {
            Category.Create("Sofa", "sofa", furniture.Id, 1, true, createdBy, utcNow),
            Category.Create("Chair", "chair", furniture.Id, 2, true, createdBy, utcNow),
            Category.Create("Table", "table", furniture.Id, 3, true, createdBy, utcNow),
        };

        await dbContext.Categories.AddRangeAsync(childCategories);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} default categories.", categories.Length + childCategories.Length);
    }
}
