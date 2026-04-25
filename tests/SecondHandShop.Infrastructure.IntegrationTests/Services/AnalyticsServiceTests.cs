using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Contracts.Analytics;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;
using SecondHandShop.Infrastructure.IntegrationTests.Infrastructure;
using SecondHandShop.Infrastructure.Persistence;
using SecondHandShop.Infrastructure.Services.Analytics;
using SecondHandShop.TestCommon.Time;

namespace SecondHandShop.Infrastructure.IntegrationTests.Services;

public class AnalyticsServiceTests(PostgresFixture db) : DatabaseTestBase(db)
{
    private static readonly DateTime UtcNow = new(2026, 4, 17, 0, 0, 0, DateTimeKind.Utc);

    [SkippableFact]
    public async Task GetOverviewAsync_ShouldReturnZeroSummary_WhenDatabaseIsEmpty()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        await ResetAnalyticsDataAsync(dbContext);
        var sut = CreateSut(Db.ConnectionString, UtcNow);

        var result = await sut.GetOverviewAsync(AnalyticsDateRange.Last30Days);

        result.Range.Should().Be(AnalyticsDateRange.Last30Days);
        result.Summary.TotalSoldItems.Should().Be(0);
        result.Summary.TotalRevenue.Should().Be(0m);
        result.Summary.TotalInquiries.Should().Be(0);
        result.SalesByCategory.Should().BeEmpty();
        result.DemandByCategory.Should().BeEmpty();
        result.SalesTrend.Should().BeEmpty();
        result.HotUnsoldProducts.Should().BeEmpty();
        result.StaleStockProducts.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task GetOverviewAsync_ShouldComputeLast30DaysAggregates()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        await ResetAnalyticsDataAsync(dbContext);
        var bagCategory = await SeedHelper.SeedCategoryAsync(dbContext, "Bags", SeedHelper.UniqueSlug("bags"));
        var coatCategory = await SeedHelper.SeedCategoryAsync(dbContext, "Coats", SeedHelper.UniqueSlug("coats"));
        var customer = await SeedHelper.SeedCustomerAsync(dbContext, email: SeedHelper.UniqueEmail("analytics"));

        var soldBag = await SeedHelper.SeedProductAsync(dbContext, bagCategory.Id, "Sold Bag", SeedHelper.UniqueSlug("soldbag"), 250m);
        var soldCoat = await SeedHelper.SeedProductAsync(dbContext, coatCategory.Id, "Sold Coat", SeedHelper.UniqueSlug("soldcoat"), 180m);
        var hotUnsold = await SeedHelper.SeedProductAsync(dbContext, bagCategory.Id, "Hot Unsold", SeedHelper.UniqueSlug("hot"), 300m);
        var staleUnsold = await SeedHelper.SeedProductAsync(dbContext, coatCategory.Id, "Stale Unsold", SeedHelper.UniqueSlug("stale"), 120m);

        var inquiry1 = await SeedInquiryAtAsync(dbContext, soldBag.Id, customer.Id, new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc));
        await SeedInquiryAtAsync(dbContext, soldCoat.Id, customer.Id, new DateTime(2026, 4, 11, 0, 0, 0, DateTimeKind.Utc));
        await SeedInquiryAtAsync(dbContext, hotUnsold.Id, customer.Id, new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc));
        await SeedInquiryAtAsync(dbContext, hotUnsold.Id, customer.Id, new DateTime(2026, 4, 13, 0, 0, 0, DateTimeKind.Utc));

        await SeedSaleAtAsync(dbContext, soldBag, customer.Id, inquiry1.Id, 220m, new DateTime(2026, 4, 14, 0, 0, 0, DateTimeKind.Utc));
        await SeedSaleAtAsync(dbContext, soldCoat, customer.Id, null, 150m, new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc));

        staleUnsold.UpdateDetails(
            staleUnsold.Title,
            staleUnsold.Slug,
            staleUnsold.Description,
            staleUnsold.Price,
            staleUnsold.CategoryId,
            null,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            staleUnsold.Condition);
        typeof(Product).GetProperty(nameof(Product.CreatedAt))!.SetValue(staleUnsold, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        await dbContext.SaveChangesAsync();

        var sut = CreateSut(Db.ConnectionString, UtcNow);

        var result = await sut.GetOverviewAsync(AnalyticsDateRange.Last30Days);

        result.Summary.TotalSoldItems.Should().Be(2);
        result.Summary.TotalRevenue.Should().Be(370m);
        result.Summary.AverageSalePrice.Should().Be(185m);
        result.Summary.TotalInquiries.Should().Be(4);
        result.Summary.InquiryToSaleConversionRate.Should().Be(0.5m);
        result.Summary.BestSellingCategoryName.Should().NotBeNull();
        result.SalesByCategory.Should().HaveCount(2);
        result.DemandByCategory.Should().ContainSingle(x => x.CategoryName == "Bags" && x.InquiryCount == 3 && x.SoldCount == 1);
        result.SalesTrend.Should().ContainSingle(x => x.MonthStartUtc == new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc) && x.SoldCount == 2);
        result.HotUnsoldProducts.Should().Contain(x => x.Title == "Hot Unsold" && x.InquiryCount == 2);
        result.StaleStockProducts.Should().Contain(x => x.Title == "Stale Unsold");
    }

    [SkippableFact]
    public async Task GetOverviewAsync_ShouldExcludeCancelledSales_FromRevenueAndCount()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        await ResetAnalyticsDataAsync(dbContext);
        var category = await SeedHelper.SeedCategoryAsync(dbContext, "Bags", SeedHelper.UniqueSlug("bags"));
        var customer = await SeedHelper.SeedCustomerAsync(dbContext, email: SeedHelper.UniqueEmail("cancelled"));
        var productA = await SeedHelper.SeedProductAsync(dbContext, category.Id, "Keeps", SeedHelper.UniqueSlug("keeps"), 200m);
        var productB = await SeedHelper.SeedProductAsync(dbContext, category.Id, "Cancelled", SeedHelper.UniqueSlug("cancelled"), 300m);

        await SeedSaleAtAsync(dbContext, productA, customer.Id, null, 180m, new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc));
        var cancelledSale = await SeedSaleAtAsync(dbContext, productB, customer.Id, null, 250m, new DateTime(2026, 4, 11, 0, 0, 0, DateTimeKind.Utc));

        cancelledSale.Cancel(SaleCancellationReason.BuyerBackedOut, "buyer-pulled-out", adminUserId: null, utcNow: new DateTime(2026, 4, 12, 0, 0, 0, DateTimeKind.Utc));
        await dbContext.SaveChangesAsync();

        var sut = CreateSut(Db.ConnectionString, UtcNow);

        var result = await sut.GetOverviewAsync(AnalyticsDateRange.Last30Days);

        result.Summary.TotalSoldItems.Should().Be(1,
            "cancelled sales are excluded from the completed-sale count");
        result.Summary.TotalRevenue.Should().Be(180m,
            "cancelled sale revenue must never be counted in summary totals");
        result.SalesByCategory.Should().ContainSingle(x => x.CategoryName == "Bags")
            .Which.TotalRevenue.Should().Be(180m);
    }

    [SkippableFact]
    public async Task GetOverviewAsync_Last7Days_ShouldUseUtcWindow_IgnoringLocalTimeZone()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        await ResetAnalyticsDataAsync(dbContext);
        var category = await SeedHelper.SeedCategoryAsync(dbContext, "Bags", SeedHelper.UniqueSlug("bags"));
        var customer = await SeedHelper.SeedCustomerAsync(dbContext, email: SeedHelper.UniqueEmail("window"));

        // UtcNow = 2026-04-17 00:00 UTC → Last7Days window ≈ 2026-04-10 00:00 UTC .. 2026-04-17 00:00 UTC.
        var insideProduct = await SeedHelper.SeedProductAsync(dbContext, category.Id, "Inside", SeedHelper.UniqueSlug("inside"), 200m);
        var outsideProduct = await SeedHelper.SeedProductAsync(dbContext, category.Id, "Outside", SeedHelper.UniqueSlug("outside"), 200m);

        await SeedSaleAtAsync(dbContext, insideProduct, customer.Id, null, 180m,
            new DateTime(2026, 4, 12, 23, 0, 0, DateTimeKind.Utc));
        // Clearly outside the 7-day window even for a +14h time zone.
        await SeedSaleAtAsync(dbContext, outsideProduct, customer.Id, null, 150m,
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));

        var sut = CreateSut(Db.ConnectionString, UtcNow);

        var result = await sut.GetOverviewAsync(AnalyticsDateRange.Last7Days);

        result.Summary.TotalSoldItems.Should().Be(1,
            "the sliding window is anchored to UtcNow and must not expand/contract with local time zone");
        result.Summary.TotalRevenue.Should().Be(180m);
    }

    [SkippableFact]
    public async Task GetOverviewAsync_ShouldIncludeOlderData_ForAllTimeRange()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        await ResetAnalyticsDataAsync(dbContext);
        var category = await SeedHelper.SeedCategoryAsync(dbContext, "Bags", SeedHelper.UniqueSlug("bags"));
        var customer = await SeedHelper.SeedCustomerAsync(dbContext, email: SeedHelper.UniqueEmail("alltime"));
        var product = await SeedHelper.SeedProductAsync(dbContext, category.Id, "Old Sale", SeedHelper.UniqueSlug("oldsale"), 200m);
        var inquiry = await SeedInquiryAtAsync(dbContext, product.Id, customer.Id, new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc));
        await SeedSaleAtAsync(dbContext, product, customer.Id, inquiry.Id, 180m, new DateTime(2025, 1, 12, 0, 0, 0, DateTimeKind.Utc));

        var sut = CreateSut(Db.ConnectionString, UtcNow);

        var last30Days = await sut.GetOverviewAsync(AnalyticsDateRange.Last30Days);
        var allTime = await sut.GetOverviewAsync(AnalyticsDateRange.AllTime);

        last30Days.Summary.TotalSoldItems.Should().Be(0);
        allTime.Summary.TotalSoldItems.Should().Be(1);
        allTime.Summary.TotalRevenue.Should().Be(180m);
        allTime.SalesTrend.Should().ContainSingle(x => x.MonthStartUtc == new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    private static AnalyticsService CreateSut(string connectionString, DateTime utcNow)
    {
        return new AnalyticsService(
            new TestDbContextFactory(connectionString),
            new FakeClock(utcNow),
            new AnalyticsOptions { AttributionWindowDays = 30 });
    }

    private static async Task ResetAnalyticsDataAsync(SecondHandShopDbContext dbContext)
    {
        await dbContext.ProductSales.ExecuteDeleteAsync();
        await dbContext.Inquiries.ExecuteDeleteAsync();
        await dbContext.InquiryIpCooldowns.ExecuteDeleteAsync();
        await dbContext.ProductImages.ExecuteDeleteAsync();
        await dbContext.ProductCategories.ExecuteDeleteAsync();
        await dbContext.Products.ExecuteDeleteAsync();
        await dbContext.Categories.ExecuteDeleteAsync();
        await dbContext.Customers.ExecuteDeleteAsync();
        await dbContext.AdminUsers.ExecuteDeleteAsync();
    }

    private static async Task<Inquiry> SeedInquiryAtAsync(
        SecondHandShopDbContext dbContext,
        Guid productId,
        Guid customerId,
        DateTime createdAtUtc)
    {
        var inquiry = Inquiry.Create(
            productId,
            customerId,
            "Alice",
            "alice@example.com",
            "021 000 000",
            "127.0.0.1",
            Guid.NewGuid().ToString("N"),
            "Is this still available?",
            createdAtUtc);
        await dbContext.Inquiries.AddAsync(inquiry);
        await dbContext.SaveChangesAsync();
        return inquiry;
    }

    private static async Task<ProductSale> SeedSaleAtAsync(
        SecondHandShopDbContext dbContext,
        Product product,
        Guid customerId,
        Guid? inquiryId,
        decimal finalSoldPrice,
        DateTime soldAtUtc)
    {
        var sale = ProductSale.Create(
            product.Id,
            product.Price,
            finalSoldPrice,
            soldAtUtc,
            null,
            soldAtUtc,
            customerId: customerId,
            inquiryId: inquiryId,
            paymentMethod: PaymentMethod.Cash);
        await dbContext.ProductSales.AddAsync(sale);
        await dbContext.SaveChangesAsync();
        return sale;
    }

    private sealed class TestDbContextFactory(string connectionString) : IDbContextFactory<SecondHandShopDbContext>
    {
        public Task<SecondHandShopDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(CreateDbContext());

        public SecondHandShopDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<SecondHandShopDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            return new SecondHandShopDbContext(options);
        }
    }
}
