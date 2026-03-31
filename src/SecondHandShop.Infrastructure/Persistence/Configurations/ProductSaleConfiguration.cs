using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Infrastructure.Persistence.Configurations;

public class ProductSaleConfiguration : IEntityTypeConfiguration<ProductSale>
{
    public void Configure(EntityTypeBuilder<ProductSale> builder)
    {
        builder.ToTable("ProductSales");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.Property(x => x.ListedPriceAtSale)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.FinalSoldPrice)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.BuyerName)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(x => x.BuyerPhone)
            .HasMaxLength(40)
            .IsRequired(false);

        builder.Property(x => x.BuyerEmail)
            .HasMaxLength(256)
            .IsRequired(false);

        builder.Property(x => x.SoldAtUtc)
            .IsRequired();

        builder.Property(x => x.PaymentMethod)
            .HasConversion<byte?>()
            .IsRequired(false);

        builder.Property(x => x.Notes)
            .HasMaxLength(2000)
            .IsRequired(false);

        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        // One sale per product (unique index)
        builder.HasIndex(x => x.ProductId)
            .IsUnique();

        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.InquiryId);
        builder.HasIndex(x => x.SoldAtUtc);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Inquiry>()
            .WithMany()
            .HasForeignKey(x => x.InquiryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<AdminUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByAdminUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<AdminUser>()
            .WithMany()
            .HasForeignKey(x => x.UpdatedByAdminUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(x =>
        {
            x.HasCheckConstraint("CK_ProductSales_FinalSoldPrice", "\"FinalSoldPrice\" >= 0");
        });
    }
}
