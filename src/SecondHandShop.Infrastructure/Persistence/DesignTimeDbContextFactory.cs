using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SecondHandShop.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SecondHandShopDbContext>
{
    private const string WebApiUserSecretsId = "499990cf-6fca-4ce3-a5d0-71df4bb8fc7f";

    public SecondHandShopDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var solutionRoot = ResolveSolutionRoot(basePath);
        var webApiPath = Path.Combine(solutionRoot, "src", "SecondHandShop.WebApi");
        var appSettingsPath = Path.Combine(webApiPath, "appsettings.json");
        var devAppSettingsPath = Path.Combine(webApiPath, "appsettings.Development.json");
        var userSecretsPath = ResolveUserSecretsPath();
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(appSettingsPath, optional: true)
            .AddJsonFile(devAppSettingsPath, optional: true)
            .AddJsonFile(userSecretsPath, optional: true)
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

    private static string ResolveSolutionRoot(string currentDirectory)
    {
        var directory = new DirectoryInfo(currentDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SecondHandShopWebsite.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return currentDirectory;
    }

    private static string ResolveUserSecretsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "Microsoft", "UserSecrets", WebApiUserSecretsId, "secrets.json");
    }
}
