using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Infrastructure.Persistence.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CloudStorageKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Url)
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(x => x.AltText)
            .HasMaxLength(300);

        builder.Property(x => x.SortOrder)
            .IsRequired();

        builder.Property(x => x.IsPrimary)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<AdminUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByAdminUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<AdminUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByAdminUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ProductId, x.SortOrder });

        builder.HasIndex(x => x.ProductId)
            .IsUnique()
            .HasFilter("[IsPrimary] = 1");
    }
}
