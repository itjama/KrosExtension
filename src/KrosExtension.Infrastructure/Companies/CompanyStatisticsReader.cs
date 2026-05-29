using KrosExtension.Application.Companies;
using KrosExtension.Application.Configuration;
using KrosExtension.Domain.Configuration;
using KrosExtension.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;

namespace KrosExtension.Infrastructure.Companies;

public sealed class CompanyStatisticsReader(ISettingsRepository settingsRepository) : ICompanyStatisticsReader
{
    public async Task<IReadOnlyList<CompanyStatisticsSummary>> GetStatisticsAsync(
        ProductType productType,
        CancellationToken cancellationToken = default)
    {
        var servers = await settingsRepository.GetDataReadServersAsync(cancellationToken);
        var trackedCompanies = await settingsRepository.GetTrackedCompaniesAsync(productType, cancellationToken);
        var result = new List<CompanyStatisticsSummary>();
        var today = DateTime.Today;

        foreach (var company in trackedCompanies)
        {
            var server = servers.FirstOrDefault(item => item.Id == company.DataReadServerId && item.IsActive);

            if (server is null)
            {
                continue;
            }

            var connectionString = DataReadServerConnectionStringFactory.CreateDatabaseConnectionString(
                server,
                productType,
                company.DatabaseName);

            if (connectionString is null)
            {
                continue;
            }

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var last7Days = await ReadActivityCountAsync(
                connection,
                productType,
                today.AddDays(-6),
                today,
                cancellationToken);

            var last30Days = await ReadActivityCountAsync(
                connection,
                productType,
                today.AddDays(-29),
                today,
                cancellationToken);

            result.Add(new CompanyStatisticsSummary(
                productType,
                company.DataReadServerId,
                server.DisplayName,
                company.DatabaseName,
                company.CompanyName,
                company.AccountingYear ?? CompanyDatabaseInfoReader.TryParseAccountingYear(company.CompanyName),
                last7Days,
                last30Days,
                decimal.Round(last30Days / 30m, 2)));
        }

        return result;
    }

    private static async Task<long> ReadActivityCountAsync(
        SqlConnection connection,
        ProductType productType,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = productType == ProductType.Omega ? OmegaActivitySql : OlympActivitySql;
        command.Parameters.Add(new SqlParameter("@FromDate", System.Data.SqlDbType.Date) { Value = fromDate.Date });
        command.Parameters.Add(new SqlParameter("@ToDate", System.Data.SqlDbType.Date) { Value = toDate.Date });

        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? 0 : Convert.ToInt64(value);
    }

    private const string OmegaActivitySql = """
        WITH activity AS (
            SELECT TRY_CONVERT(date, CONCAT(
                       e.C116_RokZaevidovania, '-',
                       e.C115_MesZaevidovania, '-',
                       e.C114_DenZaevidovania
                   )) AS ActivityDate
            FROM dbo.T040_EUD AS e
            WHERE ISNULL(e.C001_Vymazany, 0) = 0
              AND e.C023_CisloInterneKodOkruh IN ('DF', 'OF', 'DPF', 'OPF', 'zDF', 'zOF')

            UNION ALL

            SELECT TRY_CONVERT(date, CONCAT(
                       f.C062_RokVyst, '-',
                       f.C061_MesVyst, '-',
                       f.C060_DenVyst
                   )) AS ActivityDate
            FROM dbo.T228_Faktury AS f
            WHERE f.C022_KodEvidencia IN ('OF', 'OPF')
        )
        SELECT COUNT_BIG(*)
        FROM activity
        WHERE ActivityDate BETWEEN @FromDate AND @ToDate
        """;

    private const string OlympActivitySql = """
        WITH activity AS (
            SELECT CONVERT(date, StartTime) AS ActivityDate
            FROM dbo.activityLog
            WHERE StartTime >= @FromDate
              AND StartTime < DATEADD(day, 1, @ToDate)

            UNION ALL

            SELECT DATEFROMPARTS(ID_obdobie / 100, ID_obdobie % 100, 1) AS ActivityDate
            FROM (
                SELECT DISTINCT ID_obdobie
                FROM dbo.pracVycMzda
                WHERE ID_obdobie BETWEEN 190001 AND 299912
                  AND ID_obdobie % 100 BETWEEN 1 AND 12
            ) AS periods
        )
        SELECT COUNT_BIG(*)
        FROM activity
        WHERE ActivityDate BETWEEN @FromDate AND @ToDate
        """;
}
