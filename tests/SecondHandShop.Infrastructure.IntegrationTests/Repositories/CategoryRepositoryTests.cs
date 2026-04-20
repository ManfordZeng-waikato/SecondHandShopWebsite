using FluentAssertions;
using SecondHandShop.Infrastructure.IntegrationTests.Infrastructure;
using SecondHandShop.Infrastructure.Persistence.Repositories;

namespace SecondHandShop.Infrastructure.IntegrationTests.Repositories;

public class CategoryRepositoryTests(PostgresFixture db) : DatabaseTestBase(db)
{
    [SkippableFact]
    public async Task GetBySlugAsync_ShouldReturnCategory_WhenSlugExists()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var slug = SeedHelper.UniqueSlug("cat");
        var seeded = await SeedHelper.SeedCategoryAsync(dbContext, "Test Category", slug);

        var sut = new CategoryRepository(dbContext);
        var result = await sut.GetBySlugAsync(slug);

        result.Should().NotBeNull();
        result!.Id.Should().Be(seeded.Id);
        result.Name.Should().Be("Test Category");
    }

    [SkippableFact]
    public async Task GetBySlugAsync_ShouldReturnNull_WhenSlugDoesNotExist()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var sut = new CategoryRepository(dbContext);

        var result = await sut.GetBySlugAsync("nonexistent-slug-xyz");

        result.Should().BeNull();
    }

    [SkippableFact]
    public async Task SlugExistsAsync_ShouldReturnTrue_WhenSlugExists()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var slug = SeedHelper.UniqueSlug("cat");
        await SeedHelper.SeedCategoryAsync(dbContext, "Exists", slug);

        var sut = new CategoryRepository(dbContext);
        var result = await sut.SlugExistsAsync(slug);

        result.Should().BeTrue();
    }

    [SkippableFact]
    public async Task ListActiveAsync_ShouldExcludeInactiveCategories()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var activeSlug = SeedHelper.UniqueSlug("act");
        var inactiveSlug = SeedHelper.UniqueSlug("inact");
        var active = await SeedHelper.SeedCategoryAsync(dbContext, "Active", activeSlug, isActive: true);
        await SeedHelper.SeedCategoryAsync(dbContext, "Inactive", inactiveSlug, isActive: false);

        var sut = new CategoryRepository(dbContext);
        var result = await sut.ListActiveAsync();

        result.Should().Contain(c => c.Id == active.Id);
        result.Should().NotContain(c => c.Slug == inactiveSlug);
    }

    [SkippableFact]
    public async Task ListByIdsAsync_ShouldReturnOnlyMatchingCategories()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var c1 = await SeedHelper.SeedCategoryAsync(dbContext, "C1", SeedHelper.UniqueSlug("c1"));
        var c2 = await SeedHelper.SeedCategoryAsync(dbContext, "C2", SeedHelper.UniqueSlug("c2"));
        await SeedHelper.SeedCategoryAsync(dbContext, "C3", SeedHelper.UniqueSlug("c3"));

        var sut = new CategoryRepository(dbContext);
        var result = await sut.ListByIdsAsync([c1.Id, c2.Id]);

        result.Should().HaveCount(2);
        result.Select(c => c.Id).Should().BeEquivalentTo([c1.Id, c2.Id]);
    }

    [SkippableFact]
    public async Task AddAsync_ShouldPersistCategory()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var sut = new CategoryRepository(dbContext);
        var slug = SeedHelper.UniqueSlug("new");
        var category = Domain.Entities.Category.Create("New Cat", slug, null, 1, true, null, DateTime.UtcNow);

        await sut.AddAsync(category);
        await dbContext.SaveChangesAsync();

        await using var verifyDb = Db.CreateDbContext();
        var found = await new CategoryRepository(verifyDb).GetBySlugAsync(slug);
        found.Should().NotBeNull();
        found!.Name.Should().Be("New Cat");
    }
}
