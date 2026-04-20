using FluentAssertions;
using SecondHandShop.Infrastructure.IntegrationTests.Infrastructure;
using SecondHandShop.Infrastructure.Persistence.Repositories;

namespace SecondHandShop.Infrastructure.IntegrationTests.Repositories;

public class InquiryRepositoryTests(PostgresFixture db) : DatabaseTestBase(db)
{
    [SkippableFact]
    public async Task CountRecentByIpAndProductAsync_ShouldCountMatchingInquiries()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var category = await SeedHelper.SeedCategoryAsync(dbContext, "Cat", SeedHelper.UniqueSlug("cat"));
        var product = await SeedHelper.SeedProductAsync(dbContext, category.Id, slug: SeedHelper.UniqueSlug("prod"));
        var customer = await SeedHelper.SeedCustomerAsync(dbContext, email: SeedHelper.UniqueEmail("inq"));
        var ip = $"10.0.{Random.Shared.Next(0, 255)}.{Random.Shared.Next(0, 255)}";

        await SeedHelper.SeedInquiryAsync(dbContext, product.Id, customer.Id, customer.Email, ip);
        await SeedHelper.SeedInquiryAsync(dbContext, product.Id, customer.Id, customer.Email, ip);

        var sut = new InquiryRepository(dbContext);
        var count = await sut.CountRecentByIpAndProductAsync(ip, product.Id, DateTime.UtcNow.AddHours(-1));

        count.Should().Be(2);
    }

    [SkippableFact]
    public async Task CountRecentByEmailAndProductAsync_ShouldCountMatchingInquiries()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var category = await SeedHelper.SeedCategoryAsync(dbContext, "Cat", SeedHelper.UniqueSlug("cat"));
        var product = await SeedHelper.SeedProductAsync(dbContext, category.Id, slug: SeedHelper.UniqueSlug("prod"));
        var email = SeedHelper.UniqueEmail("cnt");
        var customer = await SeedHelper.SeedCustomerAsync(dbContext, email: email);

        await SeedHelper.SeedInquiryAsync(dbContext, product.Id, customer.Id, email);

        var sut = new InquiryRepository(dbContext);
        var count = await sut.CountRecentByEmailAndProductAsync(email, product.Id, DateTime.UtcNow.AddHours(-1));

        count.Should().Be(1);
    }

    [SkippableFact]
    public async Task ExistsRecentByMessageHashAsync_ShouldDetectDuplicateHashes()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var category = await SeedHelper.SeedCategoryAsync(dbContext, "Cat", SeedHelper.UniqueSlug("cat"));
        var product = await SeedHelper.SeedProductAsync(dbContext, category.Id, slug: SeedHelper.UniqueSlug("prod"));
        var customer = await SeedHelper.SeedCustomerAsync(dbContext, email: SeedHelper.UniqueEmail("hash"));

        var messageHash = Guid.NewGuid().ToString("N");
        var inquiry = Domain.Entities.Inquiry.Create(
            product.Id, customer.Id, "Alice", customer.Email, "021 000 000",
            "127.0.0.1", messageHash, "Duplicate test", DateTime.UtcNow);
        await dbContext.Inquiries.AddAsync(inquiry);
        await dbContext.SaveChangesAsync();

        var sut = new InquiryRepository(dbContext);
        var exists = await sut.ExistsRecentByMessageHashAsync(messageHash, DateTime.UtcNow.AddHours(-1));

        exists.Should().BeTrue();
    }

    [SkippableFact]
    public async Task UpsertIpCooldownAsync_ShouldInsertNewCooldown()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var ip = $"192.168.{Random.Shared.Next(0, 255)}.{Random.Shared.Next(0, 255)}";
        var blockedUntil = DateTime.UtcNow.AddMinutes(5);

        var sut = new InquiryRepository(dbContext);
        await sut.UpsertIpCooldownAsync(ip, blockedUntil, DateTime.UtcNow);
        await dbContext.SaveChangesAsync();

        var cooldownUntil = await sut.GetIpCooldownUntilAsync(ip);
        cooldownUntil.Should().BeCloseTo(blockedUntil, TimeSpan.FromSeconds(1));
    }

    [SkippableFact]
    public async Task UpsertIpCooldownAsync_ShouldUpdateExistingCooldown()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var ip = $"172.16.{Random.Shared.Next(0, 255)}.{Random.Shared.Next(0, 255)}";

        var sut = new InquiryRepository(dbContext);
        await sut.UpsertIpCooldownAsync(ip, DateTime.UtcNow.AddMinutes(5), DateTime.UtcNow);
        await dbContext.SaveChangesAsync();

        var extendedUntil = DateTime.UtcNow.AddMinutes(30);
        await sut.UpsertIpCooldownAsync(ip, extendedUntil, DateTime.UtcNow);
        await dbContext.SaveChangesAsync();

        var cooldownUntil = await sut.GetIpCooldownUntilAsync(ip);
        cooldownUntil.Should().BeCloseTo(extendedUntil, TimeSpan.FromSeconds(1));
    }

    [SkippableFact]
    public async Task GetIpCooldownUntilAsync_ShouldReturnNull_WhenNoCooldownExists()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var sut = new InquiryRepository(dbContext);

        var result = await sut.GetIpCooldownUntilAsync("1.2.3.4");

        result.Should().BeNull();
    }

    [SkippableFact]
    public async Task AddAsync_ShouldPersistInquiry()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var category = await SeedHelper.SeedCategoryAsync(dbContext, "Cat", SeedHelper.UniqueSlug("cat"));
        var product = await SeedHelper.SeedProductAsync(dbContext, category.Id, slug: SeedHelper.UniqueSlug("prod"));
        var customer = await SeedHelper.SeedCustomerAsync(dbContext, email: SeedHelper.UniqueEmail("add"));

        var inquiry = Domain.Entities.Inquiry.Create(
            product.Id, customer.Id, "Test", customer.Email, "021 000 000",
            "10.0.0.1", Guid.NewGuid().ToString("N"), "Hello", DateTime.UtcNow);

        var sut = new InquiryRepository(dbContext);
        await sut.AddAsync(inquiry);
        await dbContext.SaveChangesAsync();

        await using var verifyDb = Db.CreateDbContext();
        var found = await new InquiryRepository(verifyDb).GetByIdAsync(inquiry.Id);
        found.Should().NotBeNull();
        found!.Message.Should().Be("Hello");
    }
}
