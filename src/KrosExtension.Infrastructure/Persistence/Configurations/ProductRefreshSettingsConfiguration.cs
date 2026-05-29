using KrosExtension.Domain.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KrosExtension.Infrastructure.Persistence.Configurations;

public sealed class ProductRefreshSettingsConfiguration : IEntityTypeConfiguration<ProductRefreshSettings>
{
    public void Configure(EntityTypeBuilder<ProductRefreshSettings> builder)
    {
        builder.ToTable("ProductRefreshSettings");

        builder.HasKey(settings => settings.Id);

        builder.Property(settings => settings.ProductType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(settings => settings.RefreshTime)
            .HasConversion(
                value => value.ToTimeSpan(),
                value => TimeOnly.FromTimeSpan(value))
            .IsRequired();

        builder.HasIndex(settings => settings.ProductType)
            .IsUnique();
    }
}
