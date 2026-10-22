using KrosExtension.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KrosExtension.Infrastructure.Persistence.Configurations;

public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("AppUsers");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.UserName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(user => user.NormalizedUserName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(user => user.Role)
            .HasConversion<int>();

        builder.Property(user => user.SecurityStamp)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(user => user.NormalizedUserName)
            .IsUnique();
    }
}
