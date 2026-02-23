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
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Inquiry> Inquiries => Set<Inquiry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AdminUserConfiguration());
        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new ProductImageConfiguration());
        modelBuilder.ApplyConfiguration(new InquiryConfiguration());
    }
}
