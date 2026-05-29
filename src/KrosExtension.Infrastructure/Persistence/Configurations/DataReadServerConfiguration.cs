using KrosExtension.Domain.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KrosExtension.Infrastructure.Persistence.Configurations;

public sealed class DataReadServerConfiguration : IEntityTypeConfiguration<DataReadServer>
{
    public void Configure(EntityTypeBuilder<DataReadServer> builder)
    {
        builder.ToTable("DataReadServers");

        builder.HasKey(server => server.Id);

        builder.Property(server => server.DisplayName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(server => server.ServerName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(server => server.OlympAuthenticationMode)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(server => server.OlympUserName)
            .HasMaxLength(128);

        builder.Property(server => server.OlympPassword)
            .HasMaxLength(512);

        builder.Property(server => server.OmegaAuthenticationMode)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(server => server.OmegaUserName)
            .HasMaxLength(128);

        builder.Property(server => server.OmegaPassword)
            .HasMaxLength(512);

        builder.HasIndex(server => server.ServerName)
            .IsUnique();
    }
}
