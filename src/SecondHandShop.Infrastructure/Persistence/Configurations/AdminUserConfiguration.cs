using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Infrastructure.Persistence.Configurations;

public class AdminUserConfiguration : IEntityTypeConfiguration<AdminUser>
{
    public void Configure(EntityTypeBuilder<AdminUser> builder)
    {
        builder.ToTable("AdminUsers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DisplayName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.UserName)
            .HasMaxLength(120)
            .HasDefaultValue(string.Empty)
            .IsRequired();

        builder.Property(x => x.PasswordHash)
            .HasMaxLength(512)
            .HasDefaultValue(string.Empty)
            .IsRequired();

        builder.Property(x => x.Role)
            .HasMaxLength(50)
            .HasDefaultValue(string.Empty)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.MustChangePassword)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.TokenVersion)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.FailedLoginCount)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.LockedUntilUtc);

        builder.Property(x => x.LastSuccessfulLoginAtUtc);

        builder.Property(x => x.LastSuccessfulLoginIp)
            .HasMaxLength(64);

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(x => x.Email)
            .IsUnique();

        builder.HasIndex(x => x.UserName)
            .IsUnique()
            .HasFilter("\"UserName\" <> ''");
    }
}
