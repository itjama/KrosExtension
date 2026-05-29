using KrosExtension.Domain.Configuration;
using KrosExtension.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;

namespace KrosExtension.Infrastructure.Companies;

internal static class CompanyDatabaseInfoReader
{
    public static async Task<(string? Name, int? AccountingYear)> ReadAsync(
        DataReadServer server,
        ProductType productType,
        string databaseName,
        CancellationToken cancellationToken)
    {
        var connectionString = DataReadServerConnectionStringFactory.CreateDatabaseConnectionString(
            server,
            productType,
            databaseName);

        if (connectionString is null)
        {
            return (null, null);
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        return productType switch
        {
            ProductType.Omega => await ReadOmegaCompanyInfoAsync(connection, cancellationToken),
            ProductType.Olymp => await ReadOlympCompanyInfoAsync(connection, cancellationToken),
            _ => (null, null)
        };
    }

    public static string FormatCompanyName(string companyName, int? accountingYear)
    {
        return accountingYear is null ? companyName : $"{companyName} ({accountingYear})";
    }

    public static int? TryParseAccountingYear(string companyName)
    {
        var open = companyName.LastIndexOf('(');
        var close = companyName.LastIndexOf(')');

        if (open < 0 || close <= open)
        {
            return null;
        }

        return int.TryParse(companyName[(open + 1)..close], out var year) ? year : null;
    }

    private static async Task<(string? Name, int? AccountingYear)> ReadOmegaCompanyInfoAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                MAX(CASE WHEN C000_ID = 1010 THEN NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(max), C097_MemoA))), '') END) AS CompanyName,
                MAX(CASE WHEN C000_ID = 1003 THEN TRY_CONVERT(int, CONVERT(nvarchar(max), C097_MemoA)) END) AS AccountingYear
            FROM dbo.T000_INI
            WHERE C000_ID IN (1003, 1010)
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return (null, null);
        }

        var name = reader.IsDBNull(0) ? null : reader.GetString(0);
        var accountingYear = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1);
        return (name, accountingYear);
    }

    private static async Task<(string? Name, int? AccountingYear)> ReadOlympCompanyInfoAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (1)
                   NULLIF(LTRIM(RTRIM(orgNazov)), '') AS CompanyName,
                   CASE WHEN ID_obdobie BETWEEN 190001 AND 299912 THEN ID_obdobie / 100 END AS AccountingYear
            FROM dbo.nastavenie
            ORDER BY ID_obdobie DESC
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return (null, null);
        }

        var name = reader.IsDBNull(0) ? null : reader.GetString(0);
        var accountingYear = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1);
        return (name, accountingYear);
    }
}
