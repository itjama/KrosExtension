using KrosExtension.Domain.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KrosExtension.Infrastructure.Persistence.Configurations;

public sealed class HealthCheckRecordConfiguration : IEntityTypeConfiguration<HealthCheckRecord>
{
    public void Configure(EntityTypeBuilder<HealthCheckRecord> builder)
    {
        builder.ToTable("HealthCheckRecords");

        builder.HasKey(record => record.Id);

        builder.Property(record => record.Source)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(record => record.CheckedAt)
            .IsRequired();
    }
}
