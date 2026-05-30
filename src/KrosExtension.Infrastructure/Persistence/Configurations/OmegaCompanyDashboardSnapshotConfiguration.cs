using KrosExtension.Domain.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KrosExtension.Infrastructure.Persistence.Configurations;

public sealed class OmegaCompanyDashboardSnapshotConfiguration
    : IEntityTypeConfiguration<OmegaCompanyDashboardSnapshot>
{
    public void Configure(EntityTypeBuilder<OmegaCompanyDashboardSnapshot> builder)
    {
        builder.ToTable("OmegaCompanyDashboardSnapshots");

        builder.HasKey(snapshot => snapshot.Id);

        builder.Property(snapshot => snapshot.ServerDisplayName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(snapshot => snapshot.DatabaseName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(snapshot => snapshot.CompanyName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(snapshot => snapshot.PostingRate30Days)
            .HasPrecision(5, 2);

        builder.Property(snapshot => snapshot.TrendPercent)
            .HasPrecision(8, 2);

        builder.Property(snapshot => snapshot.PreviousMonthPostingRate)
            .HasPrecision(5, 2);

        builder.Property(snapshot => snapshot.HistoricalPostingRate)
            .HasPrecision(5, 2);

        builder.Property(snapshot => snapshot.StornoRate30Days)
            .HasPrecision(8, 2);

        builder.Property(snapshot => snapshot.WarningFlags)
            .HasMaxLength(1024)
            .IsRequired();

        builder.HasIndex(snapshot => new
            {
                snapshot.DataReadServerId,
                snapshot.DatabaseName,
                snapshot.SnapshotAt
            });

        builder.HasIndex(snapshot => snapshot.HealthScore);
    }
}
