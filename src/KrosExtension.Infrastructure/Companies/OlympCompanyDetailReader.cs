using KrosExtension.Application.Companies;
using KrosExtension.Domain.Configuration;
using KrosExtension.Infrastructure.Configuration;
using KrosExtension.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace KrosExtension.Infrastructure.Companies;

public sealed class OlympCompanyDetailReader(ApplicationDbContext dbContext) : IOlympCompanyDetailReader
{
    public async Task<OlympCompanyDetail?> GetCompanyDetailAsync(
        Guid dataReadServerId,
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        var server = await dbContext.DataReadServers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == dataReadServerId && s.IsActive, cancellationToken);

        if (server is null) return null;

        var snapshot = await dbContext.OlympCompanyDashboardSnapshots
            .AsNoTracking()
            .Where(s => s.DataReadServerId == dataReadServerId && s.DatabaseName == databaseName)
            .OrderByDescending(s => s.SnapshotAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (snapshot is null) return null;

        var connectionString = DataReadServerConnectionStringFactory.CreateDatabaseConnectionString(
            server, ProductType.Olymp, databaseName);

        if (connectionString is null) return null;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var periods = await ReadPeriodsAsync(connection, cancellationToken);

        var warnings = string.IsNullOrWhiteSpace(snapshot.WarningFlags)
            ? (IReadOnlyList<string>)[]
            : snapshot.WarningFlags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new OlympCompanyDetail(
            snapshot.DataReadServerId,
            snapshot.ServerDisplayName,
            snapshot.DatabaseName,
            snapshot.CompanyName,
            snapshot.AccountingYear,
            snapshot.SnapshotAt,
            snapshot.HealthScore,
            snapshot.ActivityScore,
            snapshot.PayrollCurrentnessScore,
            snapshot.PayrollConsistencyScore,
            snapshot.PeriodRegularityScore,
            snapshot.LastActivityDate,
            snapshot.LastProcessedPeriod,
            snapshot.CurrentMonthProcessed,
            snapshot.ProcessedPeriodsInYear,
            snapshot.Activity7Days,
            snapshot.Activity30Days,
            snapshot.NullStartTimeCount,
            snapshot.MissingPeriodsCount,
            snapshot.InconsistentPeriodsCount,
            periods,
            warnings);
    }

    private static async Task<IReadOnlyList<OlympMonthlyPeriod>> ReadPeriodsAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            WITH all_periods AS (
                SELECT DISTINCT ID_obdobie FROM dbo.pracVycMzda
                UNION
                SELECT DISTINCT ID_obdobie FROM dbo.pracVycBase
                UNION
                SELECT DISTINCT ID_obdobie FROM dbo.pracVycOdvod
            )
            SELECT
                CAST(p.ID_obdobie AS varchar(10)) AS PeriodId,
                CAST(CASE WHEN m.ID_obdobie IS NOT NULL THEN 1 ELSE 0 END AS bit) AS HasMzda,
                CAST(CASE WHEN b.ID_obdobie IS NOT NULL THEN 1 ELSE 0 END AS bit) AS HasBase,
                CAST(CASE WHEN o.ID_obdobie IS NOT NULL THEN 1 ELSE 0 END AS bit) AS HasOdvod
            FROM all_periods p
            LEFT JOIN (SELECT DISTINCT ID_obdobie FROM dbo.pracVycMzda) m ON m.ID_obdobie = p.ID_obdobie
            LEFT JOIN (SELECT DISTINCT ID_obdobie FROM dbo.pracVycBase) b ON b.ID_obdobie = p.ID_obdobie
            LEFT JOIN (SELECT DISTINCT ID_obdobie FROM dbo.pracVycOdvod) o ON o.ID_obdobie = p.ID_obdobie
            ORDER BY p.ID_obdobie DESC
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var list = new List<OlympMonthlyPeriod>();

        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new OlympMonthlyPeriod(
                reader.GetString(reader.GetOrdinal("PeriodId")),
                reader.GetBoolean(reader.GetOrdinal("HasMzda")),
                reader.GetBoolean(reader.GetOrdinal("HasBase")),
                reader.GetBoolean(reader.GetOrdinal("HasOdvod"))));
        }

        return list;
    }
}
