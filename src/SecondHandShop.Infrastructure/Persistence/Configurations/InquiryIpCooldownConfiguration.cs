using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Infrastructure.Persistence.Configurations;

public class InquiryIpCooldownConfiguration : IEntityTypeConfiguration<InquiryIpCooldown>
{
    public void Configure(EntityTypeBuilder<InquiryIpCooldown> builder)
    {
        builder.ToTable("InquiryIpCooldowns");
        builder.HasKey(x => x.IpAddress);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.BlockedUntil)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        builder.HasIndex(x => x.BlockedUntil);
    }
}
