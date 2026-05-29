using KrosExtension.Domain.Configuration;

namespace KrosExtension.Application.Companies;

public interface ICompanyStatisticsReader
{
    Task<IReadOnlyList<CompanyStatisticsSummary>> GetStatisticsAsync(
        ProductType productType,
        CancellationToken cancellationToken = default);
}
