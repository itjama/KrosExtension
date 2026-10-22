using KrosExtension.Application.Abstractions;
using KrosExtension.Domain.Configuration;
using KrosExtension.Domain.Health;
using KrosExtension.Domain.Users;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace KrosExtension.Infrastructure.Persistence;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IDataProtectionProvider? dataProtectionProvider = null)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<AvailableDatabase> AvailableDatabases => Set<AvailableDatabase>();

    public DbSet<CompanyDataSource> CompanyDataSources => Set<CompanyDataSource>();

    public DbSet<DataReadServer> DataReadServers => Set<DataReadServer>();

    public DbSet<ProductRefreshSettings> ProductRefreshSettings => Set<ProductRefreshSettings>();

    public DbSet<TrackedCompany> TrackedCompanies => Set<TrackedCompany>();

    public DbSet<HealthCheckRecord> HealthCheckRecords => Set<HealthCheckRecord>();

    public DbSet<OmegaCompanyDashboardSnapshot> OmegaCompanyDashboardSnapshots => Set<OmegaCompanyDashboardSnapshot>();

    public DbSet<OlympCompanyDashboardSnapshot> OlympCompanyDashboardSnapshots => Set<OlympCompanyDashboardSnapshot>();

    public DbSet<AppUser> Users => Set<AppUser>();

    IQueryable<AvailableDatabase> IApplicationDbContext.AvailableDatabases => AvailableDatabases;

    IQueryable<CompanyDataSource> IApplicationDbContext.CompanyDataSources => CompanyDataSources;

    IQueryable<DataReadServer> IApplicationDbContext.DataReadServers => DataReadServers;

    IQueryable<ProductRefreshSettings> IApplicationDbContext.ProductRefreshSettings => ProductRefreshSettings;

    IQueryable<TrackedCompany> IApplicationDbContext.TrackedCompanies => TrackedCompanies;

    IQueryable<HealthCheckRecord> IApplicationDbContext.HealthCheckRecords => HealthCheckRecords;

    IQueryable<OmegaCompanyDashboardSnapshot> IApplicationDbContext.OmegaCompanyDashboardSnapshots =>
        OmegaCompanyDashboardSnapshots;

    public void AddDataReadServer(DataReadServer server)
    {
        DataReadServers.Add(server);
    }

    public void RemoveDataReadServer(DataReadServer server)
    {
        DataReadServers.Remove(server);
    }

    public void AddHealthCheckRecord(HealthCheckRecord record)
    {
        HealthCheckRecords.Add(record);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        if (dataProtectionProvider is not null)
        {
            var converter = new SecretValueConverter(
                dataProtectionProvider.CreateProtector(SecretValueConverter.Purpose));
            var server = modelBuilder.Entity<DataReadServer>();
            server.Property(s => s.OlympPassword).HasConversion(converter);
            server.Property(s => s.OmegaPassword).HasConversion(converter);
        }
    }
}
