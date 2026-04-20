using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Infrastructure.IntegrationTests.Infrastructure;
using SecondHandShop.Infrastructure.Persistence.Repositories;

namespace SecondHandShop.Infrastructure.IntegrationTests.Repositories;

public class AdminUserRepositoryTests(PostgresFixture db) : DatabaseTestBase(db)
{
    [SkippableFact]
    public async Task GetByUserNameAsync_ShouldTrimUserName_AndReturnUser()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var admin = await SeedHelper.SeedAdminUserAsync(dbContext);

        var sut = new AdminUserRepository(dbContext);
        var result = await sut.GetByUserNameAsync($"  {admin.UserName}  ");

        result.Should().NotBeNull();
        result!.Id.Should().Be(admin.Id);
    }

    [SkippableFact]
    public async Task GetByIdAsync_ShouldReturnUser_WhenItExists()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var admin = await SeedHelper.SeedAdminUserAsync(dbContext);

        var sut = new AdminUserRepository(dbContext);
        var result = await sut.GetByIdAsync(admin.Id);

        result.Should().NotBeNull();
        result!.UserName.Should().Be(admin.UserName);
    }

    [SkippableFact]
    public async Task AnyAsync_ShouldReturnFalse_WhenNoAdminsExist()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        dbContext.AdminUsers.RemoveRange(await dbContext.AdminUsers.ToListAsync());
        await dbContext.SaveChangesAsync();
        var sut = new AdminUserRepository(dbContext);

        var result = await sut.AnyAsync();

        result.Should().BeFalse();
    }

    [SkippableFact]
    public async Task AddAsync_ShouldPersistUser()
    {
        EnsureDatabase();
        await using var dbContext = Db.CreateDbContext();
        var admin = AdminUser.CreateWithCredentials("newadmin", "New Admin", "hashed");

        var sut = new AdminUserRepository(dbContext);
        await sut.AddAsync(admin);
        await dbContext.SaveChangesAsync();

        await using var verifyDb = Db.CreateDbContext();
        var found = await new AdminUserRepository(verifyDb).GetByUserNameAsync("newadmin");
        found.Should().NotBeNull();
        found!.DisplayName.Should().Be("New Admin");
    }
}
