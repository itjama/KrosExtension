using KrosExtension.Domain.Configuration;

namespace KrosExtension.Application.Companies;

public interface ICompanyDataRefreshService
{
    Task<CompanyDataRefreshResult> UpdateDataAsync(
        ProductType productType,
        string? databaseName = null,
        CancellationToken cancellationToken = default);
}
