namespace SecondHandShop.Infrastructure.IntegrationTests.Infrastructure;

/// <summary>
/// Base class for tests that require a running PostgreSQL container.
/// Automatically skips all tests when Docker is unavailable.
/// </summary>
[Collection(DatabaseIntegrationCollection.Name)]
public abstract class DatabaseTestBase(PostgresFixture db)
{
    protected PostgresFixture Db { get; } = db;

    /// <summary>
    /// Call this at the start of every test (or via constructor) to skip gracefully
    /// when Docker is not running.
    /// </summary>
    protected void EnsureDatabase() => Db.SkipIfUnavailable();
}
