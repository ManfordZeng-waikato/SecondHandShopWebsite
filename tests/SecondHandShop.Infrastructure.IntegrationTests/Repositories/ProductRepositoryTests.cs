using FluentAssertions;
using SecondHandShop.Application.Contracts.Catalog;
using SecondHandShop.Infrastructure.IntegrationTests.Infrastructure;
using SecondHandShop.Infrastructure.Persistence.Repositories;

namespace SecondHandShop.Infrastructure.IntegrationTests.Repositories;

public class ProductRepositoryTests(PostgresFixture db) : DatabaseTestBase(db)
{
    [SkippableFact]
    public async Task GetByIdAsync_ShouldReturnProduct_WhenIdExists()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var category = await SeedHelper.SeedCategoryAsync(dbContext, "Cat", SeedHelper.UniqueSlug("cat"));
        var product = await SeedHelper.SeedProductAsync(dbContext, category.Id, slug: SeedHelper.UniqueSlug("prod"));

        var sut = new ProductRepository(dbContext, new TestCategoryHierarchyCache(dbContext));
        var result = await sut.GetByIdAsync(product.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(product.Id);
    }

    [SkippableFact]
    public async Task GetBySlugAsync_ShouldReturnProduct_WhenSlugExists()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var category = await SeedHelper.SeedCategoryAsync(dbContext, "Cat", SeedHelper.UniqueSlug("cat"));
        var slug = SeedHelper.UniqueSlug("slug");
        await SeedHelper.SeedProductAsync(dbContext, category.Id, slug: slug);

        var sut = new ProductRepository(dbContext, new TestCategoryHierarchyCache(dbContext));
        var result = await sut.GetBySlugAsync(slug);

        result.Should().NotBeNull();
        result!.Slug.Should().Be(slug);
    }

    [SkippableFact]
    public async Task GetPublicByIdAsync_ShouldExcludeOffShelfProducts()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var category = await SeedHelper.SeedCategoryAsync(dbContext, "Cat", SeedHelper.UniqueSlug("cat"));
        var product = await SeedHelper.SeedProductAsync(dbContext, category.Id, slug: SeedHelper.UniqueSlug("off"));
        product.OffShelf(updatedByAdminUserId: null, utcNow: DateTime.UtcNow);
        await dbContext.SaveChangesAsync();

        var sut = new ProductRepository(dbContext, new TestCategoryHierarchyCache(dbContext));
        var result = await sut.GetPublicByIdAsync(product.Id);

        result.Should().BeNull();
    }

    [SkippableFact]
    public async Task GetPublicByIdAsync_ShouldExcludeProducts_WhenCategoryIsInactive()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var category = await SeedHelper.SeedCategoryAsync(dbContext, "Dead", SeedHelper.UniqueSlug("dead"), isActive: false);
        var product = await SeedHelper.SeedProductAsync(dbContext, category.Id, slug: SeedHelper.UniqueSlug("ghost"));

        var sut = new ProductRepository(dbContext, new TestCategoryHierarchyCache(dbContext));
        var result = await sut.GetPublicByIdAsync(product.Id);

        result.Should().BeNull();
    }

    [SkippableFact]
    public async Task ListPagedForPublicAsync_ShouldReturnAvailableProducts()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var category = await SeedHelper.SeedCategoryAsync(dbContext, "Cat", SeedHelper.UniqueSlug("cat"));
        var p1 = await SeedHelper.SeedProductAsync(dbContext, category.Id, "Item A", SeedHelper.UniqueSlug("a"), 50m);
        var p2 = await SeedHelper.SeedProductAsync(dbContext, category.Id, "Item B", SeedHelper.UniqueSlug("b"), 100m);

        var sut = new ProductRepository(dbContext, new TestCategoryHierarchyCache(dbContext));
        var result = await sut.ListPagedForPublicAsync(new ProductQueryParameters { PageSize = 100 });

        result.Items.Should().Contain(x => x.Id == p1.Id);
        result.Items.Should().Contain(x => x.Id == p2.Id);
    }

    [SkippableFact]
    public async Task ListPagedForPublicAsync_ShouldFilterBySearchTitle()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var category = await SeedHelper.SeedCategoryAsync(dbContext, "Cat", SeedHelper.UniqueSlug("cat"));
        var uniqueTitle = $"UniqueSearchTarget-{Guid.NewGuid():N}"[..40];
        var p = await SeedHelper.SeedProductAsync(dbContext, category.Id, uniqueTitle, SeedHelper.UniqueSlug("srch"));

        var sut = new ProductRepository(dbContext, new TestCategoryHierarchyCache(dbContext));
        var result = await sut.ListPagedForPublicAsync(new ProductQueryParameters
        {
            Search = uniqueTitle[..20],
            PageSize = 100
        });

        result.Items.Should().Contain(x => x.Id == p.Id);
    }

    [SkippableFact]
    public async Task ListPagedForPublicAsync_ShouldFilterByPriceRange()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var category = await SeedHelper.SeedCategoryAsync(dbContext, "Cat", SeedHelper.UniqueSlug("cat"));
        var cheap = await SeedHelper.SeedProductAsync(dbContext, category.Id, "Cheap", SeedHelper.UniqueSlug("ch"), 10m);
        var expensive = await SeedHelper.SeedProductAsync(dbContext, category.Id, "Expensive", SeedHelper.UniqueSlug("ex"), 9999m);

        var sut = new ProductRepository(dbContext, new TestCategoryHierarchyCache(dbContext));
        var result = await sut.ListPagedForPublicAsync(new ProductQueryParameters
        {
            MinPrice = 5000m,
            PageSize = 100
        });

        result.Items.Should().Contain(x => x.Id == expensive.Id);
        result.Items.Should().NotContain(x => x.Id == cheap.Id);
    }

    [SkippableFact]
    public async Task ListPagedForPublicAsync_ShouldFilterByCategoryIds()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var catA = await SeedHelper.SeedCategoryAsync(dbContext, "CatA", SeedHelper.UniqueSlug("ca"));
        var catB = await SeedHelper.SeedCategoryAsync(dbContext, "CatB", SeedHelper.UniqueSlug("cb"));
        var pA = await SeedHelper.SeedProductAsync(dbContext, catA.Id, "InA", SeedHelper.UniqueSlug("ina"));
        await SeedHelper.SeedProductCategoryAsync(dbContext, pA.Id, catA.Id);
        var pB = await SeedHelper.SeedProductAsync(dbContext, catB.Id, "InB", SeedHelper.UniqueSlug("inb"));
        await SeedHelper.SeedProductCategoryAsync(dbContext, pB.Id, catB.Id);

        var sut = new ProductRepository(dbContext, new TestCategoryHierarchyCache(dbContext));
        var result = await sut.ListPagedForPublicAsync(new ProductQueryParameters
        {
            CategoryIds = catA.Id.ToString(),
            PageSize = 100
        });

        result.Items.Should().Contain(x => x.Id == pA.Id);
        result.Items.Should().NotContain(x => x.Id == pB.Id);
    }

    [SkippableFact]
    public async Task ListFeaturedForPublicAsync_ShouldReturnOnlyFeaturedAvailableProducts()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var category = await SeedHelper.SeedCategoryAsync(dbContext, "Cat", SeedHelper.UniqueSlug("cat"));
        var featured = await SeedHelper.SeedProductAsync(dbContext, category.Id, "Featured", SeedHelper.UniqueSlug("feat"));
        featured.UpdateFeaturedSettings(true, 1, null, DateTime.UtcNow);
        await dbContext.SaveChangesAsync();

        var notFeatured = await SeedHelper.SeedProductAsync(dbContext, category.Id, "Normal", SeedHelper.UniqueSlug("norm"));

        var sut = new ProductRepository(dbContext, new TestCategoryHierarchyCache(dbContext));
        var result = await sut.ListFeaturedForPublicAsync(10);

        result.Should().Contain(x => x.Id == featured.Id);
        result.Should().NotContain(x => x.Id == notFeatured.Id);
    }

    [SkippableFact]
    public async Task AddAsync_ShouldPersistProduct()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var category = await SeedHelper.SeedCategoryAsync(dbContext, "Cat", SeedHelper.UniqueSlug("cat"));
        var slug = SeedHelper.UniqueSlug("add");
        var product = Domain.Entities.Product.Create("New Product", slug, "Desc", 55m, category.Id, null, DateTime.UtcNow);

        var sut = new ProductRepository(dbContext, new TestCategoryHierarchyCache(dbContext));
        await sut.AddAsync(product);
        await dbContext.SaveChangesAsync();

        await using var verifyDb = Db.CreateDbContext();
        var found = await new ProductRepository(verifyDb, new TestCategoryHierarchyCache(verifyDb)).GetBySlugAsync(slug);
        found.Should().NotBeNull();
        found!.Title.Should().Be("New Product");
        found.Price.Should().Be(55m);
    }

    [SkippableFact]
    public async Task GetByIdWithCategoriesAsync_ShouldIncludeProductCategories()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var category = await SeedHelper.SeedCategoryAsync(dbContext, "Cat", SeedHelper.UniqueSlug("cat"));
        var product = await SeedHelper.SeedProductAsync(dbContext, category.Id, slug: SeedHelper.UniqueSlug("wc"));
        await SeedHelper.SeedProductCategoryAsync(dbContext, product.Id, category.Id);

        // Use a fresh context to avoid cached navigation properties.
        await using var readDb = Db.CreateDbContext();
        var sut = new ProductRepository(readDb, new TestCategoryHierarchyCache(readDb));
        var result = await sut.GetByIdWithCategoriesAsync(product.Id);

        result.Should().NotBeNull();
        result!.ProductCategories.Should().ContainSingle(pc => pc.CategoryId == category.Id);
    }
}
