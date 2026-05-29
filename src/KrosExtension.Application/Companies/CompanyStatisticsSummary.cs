using KrosExtension.Domain.Configuration;

namespace KrosExtension.Application.Companies;

public sealed record CompanyStatisticsSummary(
    ProductType ProductType,
    Guid DataReadServerId,
    string ServerDisplayName,
    string DatabaseName,
    string CompanyName,
    int? AccountingYear,
    long Last7DaysActivity,
    long Last30DaysActivity,
    decimal DailyAverage30Days);
