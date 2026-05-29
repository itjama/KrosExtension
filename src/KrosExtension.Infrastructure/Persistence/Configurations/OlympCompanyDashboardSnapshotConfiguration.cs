using KrosExtension.Domain.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KrosExtension.Infrastructure.Persistence.Configurations;

public sealed class OlympCompanyDashboardSnapshotConfiguration
    : IEntityTypeConfiguration<OlympCompanyDashboardSnapshot>
{
    public void Configure(EntityTypeBuilder<OlympCompanyDashboardSnapshot> builder)
    {
        builder.ToTable("OlympCompanyDashboardSnapshots");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.ServerDisplayName).HasMaxLength(256).IsRequired();
        builder.Property(s => s.DatabaseName).HasMaxLength(128).IsRequired();
        builder.Property(s => s.CompanyName).HasMaxLength(256).IsRequired();
        builder.Property(s => s.LastProcessedPeriod).HasMaxLength(16);
        builder.Property(s => s.TrendPercent).HasPrecision(8, 2);
        builder.Property(s => s.WarningFlags).HasMaxLength(1024).IsRequired();

        builder.HasIndex(s => new { s.DataReadServerId, s.DatabaseName, s.SnapshotAt });
        builder.HasIndex(s => s.HealthScore);
    }
}
