using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace SecondHandShop.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SecondHandShopDbContext>
{
    public SecondHandShopDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var appSettingsPath = Path.Combine(basePath, "src", "SecondHandShop.WebApi", "appsettings.json");
        var devAppSettingsPath = Path.Combine(basePath, "src", "SecondHandShop.WebApi", "appsettings.Development.json");
        var webApiAssembly = Assembly.Load("SecondHandShop.WebApi");
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(appSettingsPath, optional: true)
            .AddJsonFile(devAppSettingsPath, optional: true)
            .AddUserSecrets(webApiAssembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("MigrationConnection");
        connectionString = string.IsNullOrWhiteSpace(connectionString)
            ? PostgresConnectionStringResolver.Resolve(configuration)
            : PostgresConnectionStringResolver.Normalize(connectionString);

        var optionsBuilder = new DbContextOptionsBuilder<SecondHandShopDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new SecondHandShopDbContext(optionsBuilder.Options);
    }
}
