using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Infrastructure.Persistence;
using SecondHandShop.Infrastructure.Services;

namespace SecondHandShop.Infrastructure.UnitTests.Services;

public class CatalogSeedServiceTests
{
    [Fact]
    public async Task SeedDefaultCategoriesIfEmptyAsync_ShouldSeedDefaultHierarchy_WhenNoCategoriesExist()
    {
        await using var provider = BuildProvider();
        var admin = await SeedAdminAsync(provider);

        await CatalogSeedService.SeedDefaultCategoriesIfEmptyAsync(provider);

        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SecondHandShopDbContext>();
        var categories = await dbContext.Categories.ToListAsync();
        categories.Should().HaveCount(31);
        categories.Should().ContainSingle(x => x.Slug == "furniture" && x.ParentId == null);
        categories.Should().ContainSingle(x => x.Slug == "chair" && x.ParentId != null);
        categories.Should().OnlyContain(x => x.CreatedByAdminUserId == admin.Id);
    }

    [Fact]
    public async Task SeedDefaultCategoriesIfEmptyAsync_ShouldSkip_WhenCategoriesAlreadyExist()
    {
        await using var provider = BuildProvider();
        await using (var scope = provider.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<SecondHandShopDbContext>();
            await dbContext.Categories.AddAsync(Category.Create(
                "Existing",
                "existing",
                null,
                1,
                true,
                null,
                new DateTime(2026, 4, 17, 0, 0, 0, DateTimeKind.Utc)));
            await dbContext.SaveChangesAsync();
        }

        await CatalogSeedService.SeedDefaultCategoriesIfEmptyAsync(provider);

        await using var verifyScope = provider.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<SecondHandShopDbContext>();
        var categories = await verifyDb.Categories.ToListAsync();
        categories.Should().ContainSingle();
        categories[0].Slug.Should().Be("existing");
    }

    [Fact]
    public async Task SeedDefaultCategoriesIfEmptyAsync_ShouldBeIdempotent_WhenCalledTwice()
    {
        await using var provider = BuildProvider();

        await CatalogSeedService.SeedDefaultCategoriesIfEmptyAsync(provider);
        await CatalogSeedService.SeedDefaultCategoriesIfEmptyAsync(provider);

        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SecondHandShopDbContext>();
        (await dbContext.Categories.CountAsync()).Should().Be(31);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString("N");
        services.AddDbContext<SecondHandShopDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    private static async Task<AdminUser> SeedAdminAsync(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SecondHandShopDbContext>();
        var admin = AdminUser.CreateWithCredentials("lord", "Lord", "hash");
        await dbContext.AdminUsers.AddAsync(admin);
        await dbContext.SaveChangesAsync();
        return admin;
    }
}
