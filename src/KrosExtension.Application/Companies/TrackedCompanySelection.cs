using KrosExtension.Domain.Configuration;

namespace KrosExtension.Application.Companies;

public sealed record TrackedCompanySelection(
    ProductType ProductType,
    Guid DataReadServerId,
    string DatabaseName,
    string CompanyName,
    bool IsTracked);
