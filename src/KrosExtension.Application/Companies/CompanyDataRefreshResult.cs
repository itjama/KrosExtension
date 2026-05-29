using KrosExtension.Domain.Configuration;

namespace KrosExtension.Application.Companies;

public sealed record CompanyDataRefreshResult(
    ProductType ProductType,
    int RequestedCompanies,
    int RefreshedCompanies,
    IReadOnlyList<string> SkippedDatabases,
    DateTimeOffset RefreshedAt);
