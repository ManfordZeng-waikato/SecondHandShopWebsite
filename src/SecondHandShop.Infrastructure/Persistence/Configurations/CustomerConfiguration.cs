using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(120);

        builder.Property(x => x.Email)
            .HasMaxLength(256);

        builder.Property(x => x.PhoneNumber)
            .HasMaxLength(40);

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.HasIndex(x => x.Email);

        builder.HasIndex(x => x.PhoneNumber);

        builder.ToTable(x =>
        {
            x.HasCheckConstraint(
                "CK_Customers_AtLeastOneContact",
                "(NULLIF(LTRIM(RTRIM([Email])), '') IS NOT NULL) OR (NULLIF(LTRIM(RTRIM([PhoneNumber])), '') IS NOT NULL)");
        });
    }
}
