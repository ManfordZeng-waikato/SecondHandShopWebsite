using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SecondHandShop.Infrastructure.IntegrationTests.Infrastructure;

namespace SecondHandShop.Infrastructure.IntegrationTests.Repositories;

public class ProductConcurrencyTests(PostgresFixture db) : DatabaseTestBase(db)
{
    private static readonly DateTime UtcNow = new(2026, 4, 20, 12, 0, 0, DateTimeKind.Utc);

    [SkippableFact]
    public async Task UpdateDetails_ShouldThrowDbUpdateConcurrencyException_WhenRowVersionIsStale()
    {
        EnsureDatabase();

        // 1. Seed a product we will then race on in separate contexts.
        await using (var seedContext = Db.CreateDbContext())
        {
            var category = await SeedHelper.SeedCategoryAsync(seedContext, "Bags", SeedHelper.UniqueSlug("bags-concurrency"));
            await SeedHelper.SeedProductAsync(
                seedContext,
                category.Id,
                title: "Race Target",
                slug: SeedHelper.UniqueSlug("race-target"),
                price: 200m);
        }

        // 2. Two independent DbContexts both load the same row → same RowVersion in memory.
        await using var contextA = Db.CreateDbContext();
        await using var contextB = Db.CreateDbContext();

        var productA = await contextA.Products.FirstAsync(p => p.Title == "Race Target");
        var productB = await contextB.Products.FirstAsync(p => p.Title == "Race Target");

        // 3. A wins the race.
        productA.UpdateDetails(
            title: "Updated By A",
            slug: productA.Slug,
            description: productA.Description,
            price: 250m,
            categoryId: productA.CategoryId,
            updatedByAdminUserId: null,
            utcNow: UtcNow);
        await contextA.SaveChangesAsync();

        // 4. B still holds the original RowVersion — any write must be rejected.
        productB.UpdateDetails(
            title: "Updated By B",
            slug: productB.Slug,
            description: productB.Description,
            price: 300m,
            categoryId: productB.CategoryId,
            updatedByAdminUserId: null,
            utcNow: UtcNow.AddSeconds(1));

        var act = async () => await contextB.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>(
            "the second save arrives with a stale xmin/RowVersion and must be rejected so admins can resolve the conflict (maps to HTTP 409)");

        // 5. Database state reflects the winner only.
        await using var verifyContext = Db.CreateDbContext();
        var finalProduct = await verifyContext.Products.AsNoTracking().FirstAsync(p => p.Id == productA.Id);
        finalProduct.Title.Should().Be("Updated By A");
        finalProduct.Price.Should().Be(250m);
    }
}
