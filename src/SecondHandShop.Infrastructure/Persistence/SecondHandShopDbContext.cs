using Microsoft.EntityFrameworkCore;
using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Infrastructure.Persistence.Configurations;

namespace SecondHandShop.Infrastructure.Persistence;

public class SecondHandShopDbContext(DbContextOptions<SecondHandShopDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Inquiry> Inquiries => Set<Inquiry>();
    public DbSet<InquiryIpCooldown> InquiryIpCooldowns => Set<InquiryIpCooldown>();
    public DbSet<ProductSale> ProductSales => Set<ProductSale>();

    public async Task<IDatabaseTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var tx = await Database.BeginTransactionAsync(cancellationToken);
        return new EfDatabaseTransaction(tx);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AdminUserConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new ProductCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new ProductImageConfiguration());
        modelBuilder.ApplyConfiguration(new InquiryConfiguration());
        modelBuilder.ApplyConfiguration(new InquiryIpCooldownConfiguration());
        modelBuilder.ApplyConfiguration(new ProductSaleConfiguration());
    }
}
