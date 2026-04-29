using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SecondHandShop.Application.Abstractions.Security;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Infrastructure.Persistence;
using SecondHandShop.Infrastructure.Services;

namespace SecondHandShop.Infrastructure.UnitTests.Services;

public class AdminSeedServiceTests
{
    [Fact]
    public async Task SeedAdminUserAsync_ShouldCreateForcedChangeAdmin_WhenConfigIsPresentAndDatabaseIsEmpty()
    {
        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(x => x.Hash("secret-password")).Returns("hashed-secret");
        await using var provider = BuildProvider(
            new Dictionary<string, string?>
            {
                ["AdminSeed:UserName"] = "lord",
                ["AdminSeed:Password"] = "secret-password"
            },
            passwordHasher.Object);

        await AdminSeedService.SeedAdminUserAsync(provider);

        var admin = await SingleAdminAsync(provider);
        admin.UserName.Should().Be("lord");
        admin.DisplayName.Should().Be("lord");
        admin.PasswordHash.Should().Be("hashed-secret");
        admin.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task SeedAdminUserAsync_ShouldSkip_WhenSeedConfigIsMissing()
    {
        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        await using var provider = BuildProvider(new Dictionary<string, string?>(), passwordHasher.Object);

        await AdminSeedService.SeedAdminUserAsync(provider);

        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SecondHandShopDbContext>();
        (await dbContext.AdminUsers.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SeedAdminUserAsync_ShouldSkip_WhenAnyAdminAlreadyExists()
    {
        var passwordHasher = new Mock<IPasswordHasher>(MockBehavior.Strict);
        await using var provider = BuildProvider(
            new Dictionary<string, string?>
            {
                ["AdminSeed:UserName"] = "new-lord",
                ["AdminSeed:Password"] = "secret-password"
            },
            passwordHasher.Object);
        await SeedExistingAdminAsync(provider, "existing-lord", "old-hash");

        await AdminSeedService.SeedAdminUserAsync(provider);

        var admin = await SingleAdminAsync(provider);
        admin.UserName.Should().Be("existing-lord");
        admin.PasswordHash.Should().Be("old-hash");
    }

    [Fact]
    public async Task SeedAdminUserAsync_ShouldCreateAdmin_WhenOnlyE2EAdminExists()
    {
        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(x => x.Hash("e2e-password")).Returns("e2e-hash");
        passwordHasher.Setup(x => x.Hash("secret-password")).Returns("hashed-secret");
        await using var provider = BuildProvider(
            new Dictionary<string, string?>
            {
                ["AdminSeed:UserName"] = "lord",
                ["AdminSeed:Password"] = "secret-password",
                ["E2EAdminSeed:UserName"] = "playwright-admin",
                ["E2EAdminSeed:Password"] = "e2e-password"
            },
            passwordHasher.Object);

        await AdminSeedService.EnsureE2EAdminUserAsync(provider);
        await AdminSeedService.SeedAdminUserAsync(provider);

        var admins = await AllAdminsAsync(provider);
        admins.Should().HaveCount(2);
        admins.Should().Contain(admin =>
            admin.UserName == "playwright-admin" &&
            admin.DisplayName == "Playwright E2E Admin" &&
            admin.MustChangePassword == false);
        admins.Should().Contain(admin =>
            admin.UserName == "lord" &&
            admin.DisplayName == "lord" &&
            admin.PasswordHash == "hashed-secret" &&
            admin.MustChangePassword);
    }

    [Fact]
    public async Task EnsureE2EAdminUserAsync_ShouldCreateAutomationAdmin_WhenMissing()
    {
        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(x => x.Hash("e2e-password")).Returns("e2e-hash");
        await using var provider = BuildProvider(
            new Dictionary<string, string?>
            {
                ["E2EAdminSeed:UserName"] = "playwright-admin",
                ["E2EAdminSeed:Password"] = "e2e-password"
            },
            passwordHasher.Object);

        await AdminSeedService.EnsureE2EAdminUserAsync(provider);

        var admin = await SingleAdminAsync(provider);
        admin.UserName.Should().Be("playwright-admin");
        admin.DisplayName.Should().Be("Playwright E2E Admin");
        admin.PasswordHash.Should().Be("e2e-hash");
        admin.MustChangePassword.Should().BeFalse();
    }

    [Fact]
    public async Task EnsureE2EAdminUserAsync_ShouldResetExistingAutomationAdmin()
    {
        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(x => x.Hash("e2e-password")).Returns("new-hash");
        await using var provider = BuildProvider(
            new Dictionary<string, string?>
            {
                ["E2EAdminSeed:UserName"] = "playwright-admin",
                ["E2EAdminSeed:Password"] = "e2e-password"
            },
            passwordHasher.Object);
        var existing = await SeedExistingAdminAsync(provider, "playwright-admin", "old-hash", mustChangePassword: true);
        var originalTokenVersion = existing.TokenVersion;

        await AdminSeedService.EnsureE2EAdminUserAsync(provider);

        var admin = await SingleAdminAsync(provider);
        admin.PasswordHash.Should().Be("new-hash");
        admin.MustChangePassword.Should().BeFalse();
        admin.IsActive.Should().BeTrue();
        admin.TokenVersion.Should().Be(originalTokenVersion + 1);
    }

    private static ServiceProvider BuildProvider(
        IReadOnlyDictionary<string, string?> config,
        IPasswordHasher passwordHasher)
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString("N");
        services.AddDbContext<SecondHandShopDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build());
        services.AddSingleton(passwordHasher);
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    private static async Task<AdminUser> SeedExistingAdminAsync(
        ServiceProvider provider,
        string userName,
        string passwordHash,
        bool mustChangePassword = false)
    {
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SecondHandShopDbContext>();
        var admin = AdminUser.CreateWithCredentials(userName, userName, passwordHash, mustChangePassword: mustChangePassword);
        await dbContext.AdminUsers.AddAsync(admin);
        await dbContext.SaveChangesAsync();
        return admin;
    }

    private static async Task<AdminUser> SingleAdminAsync(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SecondHandShopDbContext>();
        return await dbContext.AdminUsers.SingleAsync();
    }

    private static async Task<List<AdminUser>> AllAdminsAsync(ServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SecondHandShopDbContext>();
        return await dbContext.AdminUsers.OrderBy(x => x.UserName).ToListAsync();
    }
}
