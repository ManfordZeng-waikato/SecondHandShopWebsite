using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecondHandShop.Domain.Entities;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.Property(x => x.Name)
            .HasMaxLength(120);

        builder.Property(x => x.Email)
            .HasMaxLength(256);

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(40);

        builder.Property(x => x.Status)
            .HasConversion<byte>()
            .HasDefaultValue(CustomerStatus.New)
            .HasSentinel((CustomerStatus)0)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasFilter("\"Email\" IS NOT NULL");

        builder.HasIndex(x => x.PhoneNumber)
            .IsUnique()
            .HasFilter("\"PhoneNumber\" IS NOT NULL");

        builder.ToTable(x =>
        {
            x.HasCheckConstraint(
                "CK_Customers_AtLeastOneContact",
                "(NULLIF(TRIM(\"Email\"), '') IS NOT NULL) OR (NULLIF(TRIM(\"PhoneNumber\"), '') IS NOT NULL)");
        });
    }
}
