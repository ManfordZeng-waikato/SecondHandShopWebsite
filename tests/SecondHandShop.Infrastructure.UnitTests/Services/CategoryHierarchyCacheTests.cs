using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Infrastructure.Persistence;
using SecondHandShop.Infrastructure.Services;

namespace SecondHandShop.Infrastructure.UnitTests.Services;

public class CategoryHierarchyCacheTests
{
    private static readonly DateTime UtcNow = new(2026, 4, 17, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetAsync_ShouldBuildSnapshot_WithOnlyActiveCategoriesAndDescendants()
    {
        await using var dbContext = CreateDbContext();
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var root = Category.Create("Furniture", "furniture", null, 1, true, null, UtcNow);
        var child = Category.Create("Chairs", "chairs", root.Id, 1, true, null, UtcNow);
        var inactive = Category.Create("Hidden", "hidden", root.Id, 2, false, null, UtcNow);
        await dbContext.Categories.AddRangeAsync(root, child, inactive);
        await dbContext.SaveChangesAsync();
        var sut = new CategoryHierarchyCache(dbContext, memoryCache);

        var snapshot = await sut.GetAsync();

        snapshot.Nodes.Should().HaveCount(2);
        snapshot.FindBySlug("furniture").Should().NotBeNull();
        snapshot.FindBySlug("hidden").Should().BeNull();
        snapshot.GetDescendantIds(root.Id).Should().BeEquivalentTo([root.Id, child.Id]);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnCachedSnapshot_UntilInvalidated()
    {
        await using var dbContext = CreateDbContext();
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var root = Category.Create("Furniture", "furniture", null, 1, true, null, UtcNow);
        await dbContext.Categories.AddAsync(root);
        await dbContext.SaveChangesAsync();
        var sut = new CategoryHierarchyCache(dbContext, memoryCache);

        var first = await sut.GetAsync();

        var child = Category.Create("Chairs", "chairs", root.Id, 1, true, null, UtcNow);
        await dbContext.Categories.AddAsync(child);
        await dbContext.SaveChangesAsync();
        var cached = await sut.GetAsync();
        sut.Invalidate();
        var refreshed = await sut.GetAsync();

        cached.Should().BeSameAs(first);
        cached.FindBySlug("chairs").Should().BeNull();
        refreshed.Should().NotBeSameAs(first);
        refreshed.FindBySlug("chairs").Should().NotBeNull();
    }

    [Fact]
    public async Task GetAsync_ShouldServeConcurrentCalls_FromSingleSnapshot()
    {
        await using var dbContext = CreateDbContext();
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        await dbContext.Categories.AddAsync(Category.Create("Furniture", "furniture", null, 1, true, null, UtcNow));
        await dbContext.SaveChangesAsync();
        var sut = new CategoryHierarchyCache(dbContext, memoryCache);

        var snapshots = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => sut.GetAsync()));

        snapshots.Should().OnlyContain(snapshot => snapshot.Nodes.Count == 1);
        snapshots.Distinct().Should().ContainSingle();
    }

    private static SecondHandShopDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SecondHandShopDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new SecondHandShopDbContext(options);
    }
}
