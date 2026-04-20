using Microsoft.EntityFrameworkCore;
using SecondHandShop.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SecondHandShop.Infrastructure.IntegrationTests.Infrastructure;

/// <summary>
/// Shared Postgres container for all integration tests in the assembly.
/// The container starts once and is reused; each test class should clean up
/// its own data or use transactions to isolate test state.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    /// <summary>
    /// True once the container and schema are ready. Tests should check this
    /// via <see cref="SkipIfUnavailable"/> and call <c>Skip.If</c> when false.
    /// </summary>
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

            // Apply EF migrations to set up the schema.
            await using var dbContext = CreateDbContext();
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
    /// Skip the current test when Docker/Postgres is unavailable.
    /// When the <c>REQUIRE_DOCKER</c> environment variable is set to a truthy value
    /// (e.g. in CI), the test hard-fails instead of skipping.
    /// </summary>
    public void SkipIfUnavailable()
    {
        if (IsAvailable)
            return;

        var reason = UnavailableReason ?? "Docker is not available — skipping database integration test.";

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

    public SecondHandShopDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SecondHandShopDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new SecondHandShopDbContext(options);
    }
}
