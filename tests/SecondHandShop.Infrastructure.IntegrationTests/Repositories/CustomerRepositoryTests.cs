using FluentAssertions;
using SecondHandShop.Domain.Enums;
using SecondHandShop.Infrastructure.IntegrationTests.Infrastructure;
using SecondHandShop.Infrastructure.Persistence.Repositories;

namespace SecondHandShop.Infrastructure.IntegrationTests.Repositories;

public class CustomerRepositoryTests(PostgresFixture db) : DatabaseTestBase(db)
{
    [SkippableFact]
    public async Task GetByEmailAsync_ShouldReturnCustomer_WhenEmailExists()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var email = SeedHelper.UniqueEmail("test");
        var customer = await SeedHelper.SeedCustomerAsync(dbContext, "Alice", email);

        var sut = new CustomerRepository(dbContext);
        var result = await sut.GetByEmailAsync(email);

        result.Should().NotBeNull();
        result!.Id.Should().Be(customer.Id);
    }

    [SkippableFact]
    public async Task GetByEmailAsync_ShouldReturnNull_WhenEmailDoesNotExist()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var sut = new CustomerRepository(dbContext);

        var result = await sut.GetByEmailAsync("nonexistent@example.com");

        result.Should().BeNull();
    }

    [SkippableFact]
    public async Task GetByPhoneNumberAsync_ShouldReturnCustomer_WhenPhoneExists()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var phone = $"021-{Guid.NewGuid():N}"[..15];
        var customer = await SeedHelper.SeedCustomerAsync(dbContext, phone: phone, email: null);

        var sut = new CustomerRepository(dbContext);
        var result = await sut.GetByPhoneNumberAsync(phone);

        result.Should().NotBeNull();
        result!.Id.Should().Be(customer.Id);
    }

    [SkippableFact]
    public async Task AddAsync_ShouldPersistCustomer()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var sut = new CustomerRepository(dbContext);
        var email = SeedHelper.UniqueEmail("new");
        var customer = Domain.Entities.Customer.Create("Bob", email, null, CustomerSource.Sale, DateTime.UtcNow);

        await sut.AddAsync(customer);
        await dbContext.SaveChangesAsync();

        await using var verifyDb = Db.CreateDbContext();
        var found = await new CustomerRepository(verifyDb).GetByEmailAsync(email);
        found.Should().NotBeNull();
        found!.Name.Should().Be("Bob");
        found.PrimarySource.Should().Be(CustomerSource.Sale);
    }

    [SkippableFact]
    public async Task UniqueEmailConstraint_ShouldPreventDuplicateEmails()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var email = SeedHelper.UniqueEmail("dup");
        await SeedHelper.SeedCustomerAsync(dbContext, "First", email);

        var duplicate = Domain.Entities.Customer.Create("Second", email, null, CustomerSource.Manual, DateTime.UtcNow);
        await dbContext.Customers.AddAsync(duplicate);

        var act = () => dbContext.SaveChangesAsync();

        await act.Should().ThrowAsync<Exception>();
    }

    [SkippableFact]
    public async Task GetDetailForAdminAsync_ShouldReturnProjectedDetail()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var email = SeedHelper.UniqueEmail("detail");
        var customer = await SeedHelper.SeedCustomerAsync(dbContext, "DetailTest", email);

        var sut = new CustomerRepository(dbContext);
        var result = await sut.GetDetailForAdminAsync(customer.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("DetailTest");
        result.Email.Should().Be(email);
        result.InquiryCount.Should().Be(0);
        result.PurchaseCount.Should().Be(0);
    }
}
