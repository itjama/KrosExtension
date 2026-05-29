using KrosExtension.Domain.Configuration;
using KrosExtension.Application.Companies;

namespace KrosExtension.Application.Configuration;

public interface ISettingsRepository
{
    Task<DataReadServer> AddDataReadServerAsync(
        DataReadServer server,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteDataReadServerAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> DataReadServerExistsAsync(string serverName, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AvailableDatabase>> GetAvailableDatabasesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CompanyDataSource>> GetCompanyDataSourcesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DataReadServer>> GetDataReadServersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TrackedCompany>> GetTrackedCompaniesAsync(
        ProductType productType,
        CancellationToken cancellationToken = default);

    Task<ProductRefreshSettingsDto> GetProductRefreshSettingsAsync(
        ProductType productType,
        CancellationToken cancellationToken = default);

    Task SaveProductRefreshSettingsAsync(
        ProductType productType,
        TimeOnly refreshTime,
        CancellationToken cancellationToken = default);

    Task SaveTrackedCompaniesAsync(
        ProductType productType,
        IReadOnlyList<TrackedCompanySelection> selections,
        CancellationToken cancellationToken = default);
}
