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
            .HasConversion<byte>()
            .HasDefaultValue(ProductCondition.Good)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<byte>()
            .HasDefaultValue(ProductStatus.Available)
            .IsRequired();

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
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.Slug)
            .IsUnique();

        builder.HasIndex(x => new { x.Status, x.UpdatedAt });
        builder.HasIndex(x => new { x.CategoryId, x.Status });

        builder.ToTable(x =>
        {
            x.HasCheckConstraint("CK_Products_Price", "[Price] > 0");
        });
    }
}
