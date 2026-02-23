using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Infrastructure.Persistence.Configurations;

public class InquiryConfiguration : IEntityTypeConfiguration<Inquiry>
{
    public void Configure(EntityTypeBuilder<Inquiry> builder)
    {
        builder.ToTable("Inquiries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CustomerName)
            .HasMaxLength(120);

        builder.Property(x => x.Email)
            .HasMaxLength(256);

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(40);

        builder.Property(x => x.Message)
            .HasMaxLength(3000)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.EmailDeliveryStatus)
            .HasConversion<byte>()
            .HasDefaultValue(EmailDeliveryStatus.Pending)
            .IsRequired();

        builder.Property(x => x.DeliveryError)
            .HasMaxLength(1000);

        builder.Property(x => x.EmailSendAttempts)
            .HasDefaultValue(0)
            .IsRequired();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ProductId, x.CreatedAt });
        builder.HasIndex(x => new { x.CustomerId, x.CreatedAt });

        builder.ToTable(x =>
        {
            x.HasCheckConstraint(
                "CK_Inquiries_AtLeastOneContact",
                "(NULLIF(LTRIM(RTRIM([Email])), '') IS NOT NULL) OR (NULLIF(LTRIM(RTRIM([PhoneNumber])), '') IS NOT NULL)");
        });
    }
}
