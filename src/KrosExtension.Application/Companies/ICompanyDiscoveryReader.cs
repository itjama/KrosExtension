using KrosExtension.Domain.Configuration;

namespace KrosExtension.Application.Companies;

public interface ICompanyDiscoveryReader
{
    Task<IReadOnlyList<DiscoveredCompany>> DiscoverCompaniesAsync(
        ProductType productType,
        CancellationToken cancellationToken = default);
}
