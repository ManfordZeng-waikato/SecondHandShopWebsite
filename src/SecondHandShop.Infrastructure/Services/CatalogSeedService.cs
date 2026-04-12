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

        var rootDefinitions = new (string Name, string Slug, string[][] Children)[]
        {
            ("Furniture", "furniture", new[]
            {
                new[] { "Sofa", "sofa" },
                new[] { "Chair", "chair" },
                new[] { "Table", "table" },
                new[] { "Cabinet & Storage", "cabinet-storage" },
                new[] { "Bed", "bed" },
                new[] { "Bedroom Furniture", "bedroom-furniture" },
                new[] { "Dining Furniture", "dining-furniture" },
                new[] { "Office Furniture", "office-furniture" },
                new[] { "Shelving & Bookcases", "shelving-bookcases" },
            }),
            ("Antiques & Vintage", "antiques-vintage", new[]
            {
                new[] { "Antique Furniture", "antique-furniture" },
                new[] { "Vintage Decor", "vintage-decor" },
                new[] { "Collectibles", "collectibles" },
                new[] { "Clocks", "clocks" },
                new[] { "Mirrors", "mirrors" },
                new[] { "Artwork", "artwork" },
            }),
            ("Home Decor", "home-decor", new[]
            {
                new[] { "Lamps & Lighting", "lamps-lighting" },
                new[] { "Rugs", "rugs" },
                new[] { "Wall Decor", "wall-decor" },
                new[] { "Decorative Objects", "decorative-objects" },
            }),
            ("Outdoor", "outdoor", new[]
            {
                new[] { "Outdoor Seating", "outdoor-seating" },
                new[] { "Outdoor Tables", "outdoor-tables" },
                new[] { "Garden Decor", "garden-decor" },
                new[] { "Planters", "planters" },
            }),
            ("Appliances & Household", "appliances-household", new[]
            {
                new[] { "Kitchen Appliances", "kitchen-appliances" },
                new[] { "Small Appliances", "small-appliances" },
                new[] { "Household Items", "household-items" },
            }),
        };

        var roots = rootDefinitions
            .Select((def, index) => Category.Create(def.Name, def.Slug, null, index + 1, true, createdBy, utcNow))
            .ToArray();

        await dbContext.Categories.AddRangeAsync(roots);
        await dbContext.SaveChangesAsync();

        var children = rootDefinitions
            .SelectMany((def, rootIndex) => def.Children.Select((child, childIndex) =>
                Category.Create(child[0], child[1], roots[rootIndex].Id, childIndex + 1, true, createdBy, utcNow)))
            .ToArray();

        await dbContext.Categories.AddRangeAsync(children);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} default categories.", roots.Length + children.Length);
    }
}
