using KrosExtension.Domain.Configuration;

namespace KrosExtension.Application.Companies;

public sealed record DiscoveredCompany(
    ProductType ProductType,
    Guid DataReadServerId,
    string ServerName,
    string ServerDisplayName,
    string DatabaseName,
    string CompanyName,
    bool IsTracked);
