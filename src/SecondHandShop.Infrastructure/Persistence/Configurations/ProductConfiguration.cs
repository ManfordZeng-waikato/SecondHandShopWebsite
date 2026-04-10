using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Slug)
            .HasMaxLength(220)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(x => x.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.Condition)
            .HasConversion<byte?>()
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasConversion<byte>()
            .HasDefaultValue(ProductStatus.Available)
            .HasSentinel((ProductStatus)0)
            .IsRequired();

        builder.Property(x => x.IsFeatured)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.FeaturedSortOrder)
            .IsRequired(false);

        builder.Property(x => x.CoverImageKey)
            .HasMaxLength(500);

        builder.Property(x => x.ImageCount)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.CurrentSaleId)
            .IsRequired(false);

        // Pointer to the active ProductSale row. Intentionally not a navigation property —
        // the sale-side FK (ProductSale.ProductId -> Product.Id) is the authoritative join key.
        builder.HasIndex(x => x.CurrentSaleId);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AdminUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByAdminUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<AdminUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByAdminUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.Slug)
            .IsUnique();

        builder.HasIndex(x => x.Title);

        builder.HasIndex(x => new { x.Status, x.UpdatedAt });
        builder.HasIndex(x => new { x.CategoryId, x.Status });
        builder.HasIndex(x => new { x.IsFeatured, x.Status, x.FeaturedSortOrder, x.CreatedAt });

        builder.ToTable(x =>
        {
            x.HasCheckConstraint("CK_Products_Price", "\"Price\" > 0");
            x.HasCheckConstraint(
                "CK_Products_FeaturedSortOrder_Range",
                $"\"FeaturedSortOrder\" IS NULL OR (\"FeaturedSortOrder\" >= {Product.FeaturedSortOrderMin} AND \"FeaturedSortOrder\" <= {Product.FeaturedSortOrderMax})");
        });
    }
}
