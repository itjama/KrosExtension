using KrosExtension.Domain.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KrosExtension.Infrastructure.Persistence.Configurations;

public sealed class TrackedCompanyConfiguration : IEntityTypeConfiguration<TrackedCompany>
{
    public void Configure(EntityTypeBuilder<TrackedCompany> builder)
    {
        builder.ToTable("TrackedCompanies");

        builder.HasKey(company => company.Id);

        builder.Property(company => company.ProductType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(company => company.DatabaseName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(company => company.CompanyName)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(company => company.AccountingYear);

        builder.HasIndex(company => new
            {
                company.ProductType,
                company.DataReadServerId,
                company.DatabaseName
            })
            .IsUnique();
    }
}
