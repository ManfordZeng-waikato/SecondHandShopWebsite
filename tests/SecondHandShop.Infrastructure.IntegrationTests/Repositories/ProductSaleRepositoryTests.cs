using FluentAssertions;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;
using SecondHandShop.Infrastructure.IntegrationTests.Infrastructure;
using SecondHandShop.Infrastructure.Persistence.Repositories;

namespace SecondHandShop.Infrastructure.IntegrationTests.Repositories;

public class ProductSaleRepositoryTests(PostgresFixture db) : DatabaseTestBase(db)
{
    [SkippableFact]
    public async Task GetCurrentByProductIdAsync_ShouldReturnCompletedSale()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var category = await SeedHelper.SeedCategoryAsync(dbContext, "Bags", SeedHelper.UniqueSlug("bags"));
        var product = await SeedHelper.SeedProductAsync(dbContext, category.Id, slug: SeedHelper.UniqueSlug("prod"));
        var sale = await SeedCompletedSaleAsync(dbContext, product);

        var sut = new ProductSaleRepository(dbContext);
        var result = await sut.GetCurrentByProductIdAsync(product.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(sale.Id);
        result.Status.Should().Be(SaleRecordStatus.Completed);
    }

    [SkippableFact]
    public async Task GetByIdAsync_ShouldReturnSale_WhenItExists()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var category = await SeedHelper.SeedCategoryAsync(dbContext, "Bags", SeedHelper.UniqueSlug("bags"));
        var product = await SeedHelper.SeedProductAsync(dbContext, category.Id, slug: SeedHelper.UniqueSlug("prod"));
        var sale = await SeedCompletedSaleAsync(dbContext, product);

        var sut = new ProductSaleRepository(dbContext);
        var result = await sut.GetByIdAsync(sale.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(sale.Id);
    }

    [SkippableFact]
    public async Task ListHistoryByProductIdAsync_ShouldReturnNewestSaleFirst()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var category = await SeedHelper.SeedCategoryAsync(dbContext, "Bags", SeedHelper.UniqueSlug("bags"));
        var product = await SeedHelper.SeedProductAsync(dbContext, category.Id, slug: SeedHelper.UniqueSlug("prod"));

        var olderSale = ProductSale.Create(
            product.Id,
            250m,
            200m,
            new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc),
            null,
            new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc));
        olderSale.Cancel(
            SaleCancellationReason.AdminMistake,
            "reverted",
            null,
            new DateTime(2026, 4, 11, 0, 0, 0, DateTimeKind.Utc));
        var newerSale = ProductSale.Create(
            product.Id,
            250m,
            220m,
            new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc),
            null,
            new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc));
        await dbContext.ProductSales.AddRangeAsync(olderSale, newerSale);
        await dbContext.SaveChangesAsync();

        var sut = new ProductSaleRepository(dbContext);
        var result = await sut.ListHistoryByProductIdAsync(product.Id);

        result.Select(x => x.Id).Should().Equal(newerSale.Id, olderSale.Id);
    }

    [SkippableFact]
    public async Task ListByCustomerIdAsync_ShouldExcludeCancelledSales()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var category = await SeedHelper.SeedCategoryAsync(dbContext, "Bags", SeedHelper.UniqueSlug("bags"));
        var productA = await SeedHelper.SeedProductAsync(dbContext, category.Id, "Item A", SeedHelper.UniqueSlug("a"));
        var productB = await SeedHelper.SeedProductAsync(dbContext, category.Id, "Item B", SeedHelper.UniqueSlug("b"));
        var customer = await SeedHelper.SeedCustomerAsync(dbContext, email: SeedHelper.UniqueEmail("cust"));

        var completed = ProductSale.Create(
            productA.Id,
            250m,
            220m,
            new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc),
            null,
            new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc),
            customerId: customer.Id,
            paymentMethod: PaymentMethod.Cash);
        var cancelled = ProductSale.Create(
            productB.Id,
            300m,
            280m,
            new DateTime(2026, 4, 11, 0, 0, 0, DateTimeKind.Utc),
            null,
            new DateTime(2026, 4, 11, 0, 0, 0, DateTimeKind.Utc),
            customerId: customer.Id,
            paymentMethod: PaymentMethod.BankTransfer);
        cancelled.Cancel(SaleCancellationReason.AdminMistake, "mistake", null, new DateTime(2026, 4, 13, 0, 0, 0, DateTimeKind.Utc));

        await dbContext.ProductSales.AddRangeAsync(completed, cancelled);
        await dbContext.SaveChangesAsync();

        var sut = new ProductSaleRepository(dbContext);
        var result = await sut.ListByCustomerIdAsync(customer.Id);

        result.Should().ContainSingle();
        result[0].SaleId.Should().Be(completed.Id);
        result[0].ProductTitle.Should().Be("Item A");
        result[0].PaymentMethod.Should().Be("Cash");
    }

    [SkippableFact]
    public async Task AddAsync_ShouldPersistSale()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var category = await SeedHelper.SeedCategoryAsync(dbContext, "Bags", SeedHelper.UniqueSlug("bags"));
        var product = await SeedHelper.SeedProductAsync(dbContext, category.Id, slug: SeedHelper.UniqueSlug("prod"));
        var sale = ProductSale.Create(
            product.Id,
            250m,
            220m,
            new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc),
            null,
            new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc));

        var sut = new ProductSaleRepository(dbContext);
        await sut.AddAsync(sale);
        await dbContext.SaveChangesAsync();

        await using var verifyDb = Db.CreateDbContext();
        var found = await new ProductSaleRepository(verifyDb).GetByIdAsync(sale.Id);
        found.Should().NotBeNull();
        found!.FinalSoldPrice.Should().Be(220m);
    }

    private static async Task<ProductSale> SeedCompletedSaleAsync(
        SecondHandShop.Infrastructure.Persistence.SecondHandShopDbContext dbContext,
        Product product)
    {
        var sale = ProductSale.Create(
            product.Id,
            product.Price,
            product.Price - 10m,
            new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc),
            null,
            new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc));
        await dbContext.ProductSales.AddAsync(sale);
        await dbContext.SaveChangesAsync();
        return sale;
    }
}
