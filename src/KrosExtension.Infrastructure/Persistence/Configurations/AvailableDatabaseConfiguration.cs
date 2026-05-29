using KrosExtension.Domain.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KrosExtension.Infrastructure.Persistence.Configurations;

public sealed class AvailableDatabaseConfiguration : IEntityTypeConfiguration<AvailableDatabase>
{
    public void Configure(EntityTypeBuilder<AvailableDatabase> builder)
    {
        builder.ToTable("AvailableDatabases");

        builder.HasKey(database => database.Id);

        builder.Property(database => database.ServerName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(database => database.DatabaseName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(database => database.DisplayName)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(database => new { database.ServerName, database.DatabaseName })
            .IsUnique();
    }
}
