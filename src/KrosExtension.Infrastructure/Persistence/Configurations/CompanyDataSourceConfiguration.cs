using KrosExtension.Domain.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KrosExtension.Infrastructure.Persistence.Configurations;

public sealed class CompanyDataSourceConfiguration : IEntityTypeConfiguration<CompanyDataSource>
{
    public void Configure(EntityTypeBuilder<CompanyDataSource> builder)
    {
        builder.ToTable("CompanyDataSources");

        builder.HasKey(source => source.Id);

        builder.Property(source => source.CompanyCode)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(source => source.CompanyName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(source => source.ServerName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(source => source.DatabaseName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(source => source.ReadOnlyConnectionString)
            .HasMaxLength(2048)
            .IsRequired();

        builder.HasIndex(source => source.CompanyCode)
            .IsUnique();
    }
}
