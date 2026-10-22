using KrosExtension.Application.Companies;
using KrosExtension.Domain.Configuration;
using KrosExtension.Infrastructure.Configuration;
using KrosExtension.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace KrosExtension.Infrastructure.Companies;

public sealed class OmegaCompanyDetailReader(ApplicationDbContext dbContext) : IOmegaCompanyDetailReader
{
    public async Task<OmegaCompanyDetail?> GetCompanyDetailAsync(
        Guid dataReadServerId,
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        var server = await dbContext.DataReadServers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == dataReadServerId && s.IsActive, cancellationToken);

        if (server is null)
        {
            return null;
        }

        var snapshot = await dbContext.OmegaCompanyDashboardSnapshots
            .AsNoTracking()
            .Where(s => s.DataReadServerId == dataReadServerId && s.DatabaseName == databaseName)
            .OrderByDescending(s => s.SnapshotAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (snapshot is null)
        {
            return null;
        }

        var connectionString = DataReadServerConnectionStringFactory.CreateDatabaseConnectionString(
            server, ProductType.Omega, databaseName);

        if (connectionString is null)
        {
            return null;
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var funnel = await ReadFunnelAsync(connection, DateTime.Today, cancellationToken);
        var monthly = await ReadMonthlyAsync(connection, DateTime.Today, cancellationToken);
        var postingLagOver30 = await ReadPostingLagOver30Async(connection, DateTime.Today, cancellationToken);

        var warnings = string.IsNullOrWhiteSpace(snapshot.WarningFlags)
            ? (IReadOnlyList<string>)[]
            : snapshot.WarningFlags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new OmegaCompanyDetail(
            snapshot.DataReadServerId,
            snapshot.ServerDisplayName,
            snapshot.DatabaseName,
            snapshot.CompanyName,
            snapshot.AccountingYear,
            snapshot.SnapshotAt,
            snapshot.HealthScore,
            snapshot.ActivityScore,
            snapshot.CompletionScore,
            snapshot.BacklogAgingScore,
            snapshot.MonthClosingScore,
            snapshot.DataQualityScore,
            snapshot.TimelinessScore,
            funnel.Registered30d,
            funnel.PreAccounted30d,
            funnel.Posted30d,
            funnel.Platform30d,
            funnel.Imported30d,
            funnel.Manual30d,
            funnel.Bucket0to3d + funnel.Bucket4to7d + funnel.Bucket8to30d + funnel.Bucket30dPlus,
            funnel.Bucket0to3d,
            funnel.Bucket4to7d,
            funnel.Bucket8to30d,
            funnel.Bucket30dPlus,
            monthly,
            warnings,
            snapshot.PostingLagDfAvgDays,
            snapshot.PostingLagDfMaxDays,
            snapshot.PostingLagDfCount,
            postingLagOver30.DfOver30Count,
            snapshot.PostingLagOfAvgDays,
            snapshot.PostingLagOfMaxDays,
            snapshot.PostingLagOfCount,
            postingLagOver30.OfOver30Count,
            snapshot.PostingLagPdAvgDays,
            snapshot.PostingLagPdMaxDays,
            snapshot.PostingLagPdCount,
            postingLagOver30.PdOver30Count,
            snapshot.PostingLagBvAvgDays,
            snapshot.PostingLagBvMaxDays,
            snapshot.PostingLagBvCount,
            postingLagOver30.BvOver30Count,
            snapshot.LateEntryDfAvgDays,
            snapshot.LateEntryDfMaxDays,
            snapshot.LateEntryDfCount,
            snapshot.LateEntryOfAvgDays,
            snapshot.LateEntryOfMaxDays,
            snapshot.LateEntryOfCount,
            snapshot.LateEntryPdAvgDays,
            snapshot.LateEntryPdMaxDays,
            snapshot.LateEntryPdCount);
    }

    private static async Task<FunnelMetrics> ReadFunnelAsync(
        SqlConnection connection,
        DateTime today,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = FunnelSql;
        command.Parameters.Add(new SqlParameter("@Today", SqlDbType.Date) { Value = today.Date });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return new FunnelMetrics(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        return new FunnelMetrics(
            GetInt64(reader, "Registered30d"),
            GetInt64(reader, "PreAccounted30d"),
            GetInt64(reader, "Posted30d"),
            GetInt64(reader, "Platform30d"),
            GetInt64(reader, "Imported30d"),
            GetInt64(reader, "Manual30d"),
            GetInt64(reader, "Bucket0to3d"),
            GetInt64(reader, "Bucket4to7d"),
            GetInt64(reader, "Bucket8to30d"),
            GetInt64(reader, "Bucket30dPlus"));
    }

    private static async Task<IReadOnlyList<OmegaMonthlyBreakdown>> ReadMonthlyAsync(
        SqlConnection connection,
        DateTime today,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = MonthlySql;
        command.Parameters.Add(new SqlParameter("@Today", SqlDbType.Date) { Value = today.Date });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var list = new List<OmegaMonthlyBreakdown>();

        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new OmegaMonthlyBreakdown(
                reader.GetInt32(reader.GetOrdinal("Year")),
                reader.GetInt32(reader.GetOrdinal("Month")),
                GetInt64(reader, "Registered"),
                GetInt64(reader, "Posted"),
                GetNullableDecimal(reader, "PostingRate")));
        }

        return list;
    }

    private static async Task<PostingLagOver30Metrics> ReadPostingLagOver30Async(
        SqlConnection connection,
        DateTime today,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = PostingLagOver30Sql;
        command.Parameters.Add(new SqlParameter("@Today", SqlDbType.Date) { Value = today.Date });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return new PostingLagOver30Metrics(0, 0, 0, 0);
        }

        return new PostingLagOver30Metrics(
            GetInt64(reader, "DfOver30Count"),
            GetInt64(reader, "OfOver30Count"),
            GetInt64(reader, "PdOver30Count"),
            GetInt64(reader, "BvOver30Count"));
    }

    private static long GetInt64(SqlDataReader reader, string name)
    {
        var value = reader[name];
        return value is null or DBNull ? 0 : Convert.ToInt64(value);
    }

    private static decimal? GetNullableDecimal(SqlDataReader reader, string name)
    {
        var value = reader[name];
        return value is null or DBNull ? null : Convert.ToDecimal(value);
    }

    private sealed record PostingLagOver30Metrics(
        long DfOver30Count,
        long OfOver30Count,
        long PdOver30Count,
        long BvOver30Count);

    private sealed record FunnelMetrics(
        long Registered30d,
        long PreAccounted30d,
        long Posted30d,
        long Platform30d,
        long Imported30d,
        long Manual30d,
        long Bucket0to3d,
        long Bucket4to7d,
        long Bucket8to30d,
        long Bucket30dPlus);

    private const string PostingLagOver30Sql = """
        DECLARE @From90 date = DATEADD(day, -89, @Today);

        SELECT
            COUNT(CASE WHEN DocumentCode = 'DF' AND PostingLagDays > 30 THEN 1 END) AS DfOver30Count,
            COUNT(CASE WHEN DocumentCode = 'OF' AND PostingLagDays > 30 THEN 1 END) AS OfOver30Count,
            COUNT(CASE WHEN DocumentCode = 'PD' AND PostingLagDays > 30 THEN 1 END) AS PdOver30Count,
            COUNT(CASE WHEN DocumentCode = 'BV' AND PostingLagDays > 30 THEN 1 END) AS BvOver30Count
        FROM (
            SELECT
                e.C023_CisloInterneKodOkruh AS DocumentCode,
                DATEDIFF(day,
                    TRY_CONVERT(date, CONCAT(e.C116_RokZaevidovania, '-', e.C115_MesZaevidovania, '-', e.C114_DenZaevidovania)),
                    TRY_CONVERT(date, CONCAT(e.C122_RokUzavierkyHLK, '-', e.C121_MesUzavierkyHLK, '-', e.C120_DenUzavierkyHLK))
                ) AS PostingLagDays
            FROM dbo.T040_EUD AS e
            WHERE ISNULL(e.C001_Vymazany, 0) = 0
              AND e.C023_CisloInterneKodOkruh IN ('DF', 'OF', 'PD', 'BV')
              AND NULLIF(LTRIM(RTRIM(e.C027_KodZauctoval)), '') IS NOT NULL
              AND TRY_CONVERT(date, CONCAT(e.C122_RokUzavierkyHLK, '-', e.C121_MesUzavierkyHLK, '-', e.C120_DenUzavierkyHLK))
                  BETWEEN @From90 AND @Today
        ) AS src
        """;

    private const string FunnelSql = """
        DECLARE @From30 date = DATEADD(day, -29, @Today);

        WITH documents AS (
            SELECT
                TRY_CONVERT(date, CONCAT(
                    e.C116_RokZaevidovania, '-',
                    e.C115_MesZaevidovania, '-',
                    e.C114_DenZaevidovania
                )) AS RegisteredDate,
                CASE WHEN NULLIF(LTRIM(RTRIM(e.C027_KodZauctoval)), '') IS NULL
                     THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS IsPosted,
                CASE WHEN e.C108_PREdkontovany <> 0
                     THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsPreAccounted,
                CASE WHEN NULLIF(LTRIM(RTRIM(e.C187_PlatformaId)), '') IS NULL
                     THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS HasPlatformId,
                e.C138_PovodDokladu AS OriginCode
            FROM dbo.T040_EUD AS e
            WHERE ISNULL(e.C001_Vymazany, 0) = 0
              AND e.C023_CisloInterneKodOkruh IN ('DF', 'OF', 'DPF', 'OPF', 'zDF', 'zOF')
        ),
        funnel AS (
            SELECT
                COUNT(*) AS Registered30d,
                SUM(CASE WHEN IsPreAccounted = 1 THEN 1 ELSE 0 END) AS PreAccounted30d,
                SUM(CASE WHEN IsPosted = 1 THEN 1 ELSE 0 END) AS Posted30d,
                SUM(CASE WHEN HasPlatformId = 1 THEN 1 ELSE 0 END) AS Platform30d,
                SUM(CASE WHEN HasPlatformId = 0 AND ISNULL(OriginCode, 0) <> 0 THEN 1 ELSE 0 END) AS Imported30d,
                SUM(CASE WHEN HasPlatformId = 0 AND ISNULL(OriginCode, 0) = 0 THEN 1 ELSE 0 END) AS Manual30d
            FROM documents
            WHERE RegisteredDate BETWEEN @From30 AND @Today
        ),
        backlog AS (
            SELECT
                SUM(CASE WHEN DATEDIFF(day, RegisteredDate, @Today) <= 3 THEN 1 ELSE 0 END) AS Bucket0to3d,
                SUM(CASE WHEN DATEDIFF(day, RegisteredDate, @Today) BETWEEN 4 AND 7 THEN 1 ELSE 0 END) AS Bucket4to7d,
                SUM(CASE WHEN DATEDIFF(day, RegisteredDate, @Today) BETWEEN 8 AND 30 THEN 1 ELSE 0 END) AS Bucket8to30d,
                SUM(CASE WHEN DATEDIFF(day, RegisteredDate, @Today) > 30 THEN 1 ELSE 0 END) AS Bucket30dPlus
            FROM documents
            WHERE IsPosted = 0
              AND RegisteredDate IS NOT NULL
        )
        SELECT
            f.Registered30d, f.PreAccounted30d, f.Posted30d,
            f.Platform30d, f.Imported30d, f.Manual30d,
            b.Bucket0to3d, b.Bucket4to7d, b.Bucket8to30d, b.Bucket30dPlus
        FROM funnel AS f
        CROSS JOIN backlog AS b
        """;

    private const string MonthlySql = """
        WITH documents AS (
            SELECT
                TRY_CONVERT(date, CONCAT(
                    e.C116_RokZaevidovania, '-',
                    e.C115_MesZaevidovania, '-',
                    e.C114_DenZaevidovania
                )) AS RegisteredDate,
                CASE WHEN NULLIF(LTRIM(RTRIM(e.C027_KodZauctoval)), '') IS NULL
                     THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS IsPosted
            FROM dbo.T040_EUD AS e
            WHERE ISNULL(e.C001_Vymazany, 0) = 0
              AND e.C023_CisloInterneKodOkruh IN ('DF', 'OF', 'DPF', 'OPF', 'zDF', 'zOF')
        )
        SELECT
            YEAR(RegisteredDate)  AS Year,
            MONTH(RegisteredDate) AS Month,
            COUNT(*) AS Registered,
            SUM(CASE WHEN IsPosted = 1 THEN 1 ELSE 0 END) AS Posted,
            CASE WHEN COUNT(*) = 0 THEN NULL
                 ELSE CONVERT(decimal(5, 2),
                      SUM(CASE WHEN IsPosted = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*))
            END AS PostingRate
        FROM documents
        WHERE RegisteredDate IS NOT NULL
          AND RegisteredDate >= DATEADD(month, -11, DATEFROMPARTS(YEAR(@Today), MONTH(@Today), 1))
          AND RegisteredDate <  DATEADD(month,   1, DATEFROMPARTS(YEAR(@Today), MONTH(@Today), 1))
        GROUP BY YEAR(RegisteredDate), MONTH(RegisteredDate)
        ORDER BY Year, Month
        """;
}
