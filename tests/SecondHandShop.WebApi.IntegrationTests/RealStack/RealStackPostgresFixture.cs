using Microsoft.EntityFrameworkCore;
using SecondHandShop.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SecondHandShop.WebApi.IntegrationTests.RealStack;

/// <summary>
/// Starts a disposable Postgres container for the RealStack smoke suite.
/// Mirrors <c>PostgresFixture</c> in the Infrastructure.IntegrationTests assembly
/// but is local here so the WebApi test project stays self-contained.
/// </summary>
public sealed class RealStackPostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public bool IsAvailable { get; private set; }
    public string? UnavailableReason { get; private set; }

    public string ConnectionString =>
        _container?.GetConnectionString()
        ?? throw new InvalidOperationException("Postgres container is not available.");

    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:17-alpine")
                .Build();

            await _container.StartAsync();

            // Apply EF migrations so /api/* hits a fully-schemaed database.
            var options = new DbContextOptionsBuilder<SecondHandShopDbContext>()
                .UseNpgsql(_container.GetConnectionString())
                .Options;
            await using var dbContext = new SecondHandShopDbContext(options);
            await dbContext.Database.MigrateAsync();

            IsAvailable = true;
        }
        catch (Exception ex)
        {
            UnavailableReason = $"Docker/Postgres unavailable: {ex.Message}";
            IsAvailable = false;
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }

    /// <summary>
    /// Skips when Docker is unavailable. CI pipelines set REQUIRE_DOCKER=true
    /// so missing infrastructure hard-fails instead of silently skipping.
    /// </summary>
    public void SkipIfUnavailable()
    {
        if (IsAvailable)
            return;

        var reason = UnavailableReason ?? "Docker is not available — skipping real-stack E2E test.";
        if (IsDockerRequired())
            Assert.Fail(reason + " (REQUIRE_DOCKER=true; refusing to skip.)");

        Skip.If(true, reason);
    }

    private static bool IsDockerRequired()
    {
        var value = Environment.GetEnvironmentVariable("REQUIRE_DOCKER");
        return !string.IsNullOrWhiteSpace(value)
            && (value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("1", StringComparison.Ordinal)
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase));
    }
}

[CollectionDefinition("WebApiRealStack")]
public sealed class RealStackCollection : ICollectionFixture<RealStackPostgresFixture>
{
}
