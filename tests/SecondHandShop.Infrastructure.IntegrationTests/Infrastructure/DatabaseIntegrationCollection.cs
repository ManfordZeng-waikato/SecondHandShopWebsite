namespace SecondHandShop.Infrastructure.IntegrationTests.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DatabaseIntegrationCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "DatabaseIntegration";
}
