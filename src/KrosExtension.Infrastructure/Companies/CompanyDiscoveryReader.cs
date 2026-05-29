using System.Text.RegularExpressions;
using KrosExtension.Application.Companies;
using KrosExtension.Application.Configuration;
using KrosExtension.Domain.Configuration;
using KrosExtension.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;

namespace KrosExtension.Infrastructure.Companies;

public sealed partial class CompanyDiscoveryReader(ISettingsRepository settingsRepository)
    : ICompanyDiscoveryReader
{
    public async Task<IReadOnlyList<DiscoveredCompany>> DiscoverCompaniesAsync(
        ProductType productType,
        CancellationToken cancellationToken = default)
    {
        var servers = await settingsRepository.GetDataReadServersAsync(cancellationToken);
        var trackedCompanies = await settingsRepository.GetTrackedCompaniesAsync(productType, cancellationToken);
        var result = new List<DiscoveredCompany>();

        foreach (var server in servers.Where(server => server.IsActive))
        {
            var connectionString = DataReadServerConnectionStringFactory.CreateMasterConnectionString(
                server,
                productType);

            if (connectionString is null)
            {
                continue;
            }

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT name
                FROM sys.databases
                WHERE database_id > 4 AND state_desc = 'ONLINE'
                ORDER BY name
                """;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var databaseName = reader.GetString(0);

                if (!IsProductDatabase(productType, databaseName))
                {
                    continue;
                }

                var isTracked = trackedCompanies.Any(company =>
                    company.DataReadServerId == server.Id &&
                    string.Equals(company.DatabaseName, databaseName, StringComparison.OrdinalIgnoreCase));

                var companyInfo = await CompanyDatabaseInfoReader.ReadAsync(
                    server,
                    productType,
                    databaseName,
                    cancellationToken);

                result.Add(new DiscoveredCompany(
                    productType,
                    server.Id,
                    server.ServerName,
                    server.DisplayName,
                    databaseName,
                    CompanyDatabaseInfoReader.FormatCompanyName(
                        companyInfo.Name ?? GetCompanyName(productType, databaseName),
                        companyInfo.AccountingYear),
                    isTracked));
            }
        }

        return result;
    }

    private static bool IsProductDatabase(ProductType productType, string databaseName)
    {
        return productType switch
        {
            ProductType.Olymp => databaseName.StartsWith("mzdy", StringComparison.OrdinalIgnoreCase),
            ProductType.Omega => OmegaDatabaseNameRegex().IsMatch(databaseName),
            _ => false
        };
    }

    private static string GetCompanyName(ProductType productType, string databaseName)
    {
        return productType switch
        {
            ProductType.Olymp when databaseName.Length > 4 => databaseName[4..],
            ProductType.Omega => databaseName,
            _ => databaseName
        };
    }

    [GeneratedRegex("^x_[0-9]{9}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OmegaDatabaseNameRegex();
}
