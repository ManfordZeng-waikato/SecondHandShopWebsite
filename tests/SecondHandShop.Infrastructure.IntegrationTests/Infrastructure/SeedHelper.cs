using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;
using SecondHandShop.Infrastructure.Persistence;

namespace SecondHandShop.Infrastructure.IntegrationTests.Infrastructure;

/// <summary>
/// Provides convenience methods for inserting seed data into the test database.
/// Each method creates, adds, and saves the entity so the caller gets a tracked
/// entity with a database-assigned RowVersion.
/// </summary>
internal static class SeedHelper
{
    private static readonly DateTime UtcNow = new(2026, 4, 17, 0, 0, 0, DateTimeKind.Utc);

    public static async Task<AdminUser> SeedAdminUserAsync(
        SecondHandShopDbContext db,
        string? userName = null)
    {
        var effectiveUserName = userName ?? UniqueSlug("admin");
        var admin = AdminUser.CreateWithCredentials(effectiveUserName, "Test Admin", "hashed");
        await db.AdminUsers.AddAsync(admin);
        await db.SaveChangesAsync();
        return admin;
    }

    public static async Task<Category> SeedCategoryAsync(
        SecondHandShopDbContext db,
        string name = "Furniture",
        string slug = "furniture",
        Guid? parentId = null,
        bool isActive = true)
    {
        var category = Category.Create(name, slug, parentId, 1, isActive, null, UtcNow);
        await db.Categories.AddAsync(category);
        await db.SaveChangesAsync();
        return category;
    }

    public static async Task<Product> SeedProductAsync(
        SecondHandShopDbContext db,
        Guid categoryId,
        string title = "Vintage Lamp",
        string slug = "vintage-lamp",
        decimal price = 99.99m,
        ProductCondition? condition = ProductCondition.Good)
    {
        var product = Product.Create(title, slug, "A nice lamp.", price, categoryId, null, UtcNow, condition);
        await db.Products.AddAsync(product);
        await db.SaveChangesAsync();
        return product;
    }

    public static async Task<Customer> SeedCustomerAsync(
        SecondHandShopDbContext db,
        string? name = "Alice",
        string? email = "alice@example.com",
        string? phone = null,
        CustomerSource source = CustomerSource.Inquiry)
    {
        var customer = Customer.Create(name, email, phone, source, UtcNow);
        await db.Customers.AddAsync(customer);
        await db.SaveChangesAsync();
        return customer;
    }

    public static async Task<Inquiry> SeedInquiryAsync(
        SecondHandShopDbContext db,
        Guid productId,
        Guid customerId,
        string? email = "alice@example.com",
        string? ip = "127.0.0.1",
        string message = "Is this still available?")
    {
        var inquiry = Inquiry.Create(
            productId, customerId, "Alice", email, "021 000 000", ip,
            Guid.NewGuid().ToString("N"), message, DateTime.UtcNow);
        await db.Inquiries.AddAsync(inquiry);
        await db.SaveChangesAsync();
        return inquiry;
    }

    public static async Task<ProductCategory> SeedProductCategoryAsync(
        SecondHandShopDbContext db,
        Guid productId,
        Guid categoryId)
    {
        var pc = ProductCategory.Create(productId, categoryId);
        await db.ProductCategories.AddAsync(pc);
        await db.SaveChangesAsync();
        return pc;
    }

    public static string UniqueSlug(string prefix = "item") => $"{prefix}-{Guid.NewGuid():N}"[..30];

    /// <summary>
    /// Generates a unique, valid email address for test data.
    /// Format: {prefix}-{8-char-hex}@test.com (always under 256 chars).
    /// </summary>
    public static string UniqueEmail(string prefix = "u") => $"{prefix}-{Guid.NewGuid().ToString("N")[..8]}@test.com";
}
