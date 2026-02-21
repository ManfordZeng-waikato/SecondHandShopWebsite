using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SecondHandShop.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SecondHandShopDbContext>
{
    public SecondHandShopDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SecondHandShopDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\MSSQLLocalDB;Database=SecondHandShopDb;Trusted_Connection=True;TrustServerCertificate=True;");

        return new SecondHandShopDbContext(optionsBuilder.Options);
    }
}
