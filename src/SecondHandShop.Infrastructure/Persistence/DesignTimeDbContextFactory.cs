using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SecondHandShop.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SecondHandShopDbContext>
{
    public SecondHandShopDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SecondHandShopDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Database=SecondHandShopDb;Username=postgres;Password=postgres;");

        return new SecondHandShopDbContext(optionsBuilder.Options);
    }
}
