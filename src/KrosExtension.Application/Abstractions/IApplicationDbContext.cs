using KrosExtension.Domain.Configuration;
using KrosExtension.Domain.Health;

namespace KrosExtension.Application.Abstractions;

public interface IApplicationDbContext
{
    IQueryable<AvailableDatabase> AvailableDatabases { get; }

    IQueryable<CompanyDataSource> CompanyDataSources { get; }

    IQueryable<DataReadServer> DataReadServers { get; }

    IQueryable<ProductRefreshSettings> ProductRefreshSettings { get; }

    IQueryable<TrackedCompany> TrackedCompanies { get; }

    IQueryable<HealthCheckRecord> HealthCheckRecords { get; }

    IQueryable<OmegaCompanyDashboardSnapshot> OmegaCompanyDashboardSnapshots { get; }

    void AddDataReadServer(DataReadServer server);

    void RemoveDataReadServer(DataReadServer server);

    void AddHealthCheckRecord(HealthCheckRecord record);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
