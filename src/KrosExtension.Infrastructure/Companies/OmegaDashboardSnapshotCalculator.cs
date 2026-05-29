using KrosExtension.Domain.Configuration;
using KrosExtension.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;

namespace KrosExtension.Infrastructure.Companies;

internal static class OmegaDashboardSnapshotCalculator
{
    public static async Task<OmegaCompanyDashboardSnapshot?> ReadSnapshotAsync(
        DataReadServer server,
        TrackedCompany company,
        DateTime today,
        CancellationToken cancellationToken)
    {
        var connectionString = DataReadServerConnectionStringFactory.CreateDatabaseConnectionString(
            server,
            ProductType.Omega,
            company.DatabaseName);

        if (connectionString is null)
        {
            return null;
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = SnapshotSql;
        command.Parameters.Add(new SqlParameter("@Today", System.Data.SqlDbType.Date) { Value = today.Date });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var metrics = new OmegaDashboardMetrics(
            GetInt64(reader, "Registered7Days") + GetInt64(reader, "Posted7Days"),
            GetInt64(reader, "Registered30Days") + GetInt64(reader, "Posted30Days"),
            GetInt64(reader, "PreviousRegistered7Days") + GetInt64(reader, "PreviousPosted7Days"),
            GetInt64(reader, "Registered30Days"),
            GetInt64(reader, "Posted30Days"),
            GetInt64(reader, "BacklogCount"),
            GetInt64(reader, "Backlog7DaysPlus"),
            GetInt64(reader, "Backlog30DaysPlus"),
            GetNullableInt32(reader, "OldestOpenAgeDays"),
            GetNullableDateTime(reader, "LastActivityDate"),
            GetNullableDecimal(reader, "PreviousMonthPostingRate"),
            GetNullableDecimal(reader, "HistoricalPostingRate"),
            GetInt64(reader, "Imported30Days"),
            GetInt64(reader, "Platform30Days"),
            GetInt64(reader, "Manual30Days"),
            GetInt64(reader, "DuplicateDocumentLinks"),
            GetInt64(reader, "NullDateCount"),
            GetInt64(reader, "OrphanLineCount"),
            GetInt64(reader, "OrphanPaymentCount"),
            GetInt64(reader, "UnknownCircuitCount"));

        var scores = CalculateScores(metrics);
        var warnings = BuildWarnings(metrics);
        var trendPercent = CalculateTrendPercent(metrics.Activity7Days, metrics.Previous7DaysActivity);
        var postingRate30Days = CalculateRate(metrics.Posted30Days, metrics.Registered30Days);

        return new OmegaCompanyDashboardSnapshot(
            company.DataReadServerId,
            server.DisplayName,
            company.DatabaseName,
            company.CompanyName,
            company.AccountingYear,
            DateTimeOffset.UtcNow,
            scores.HealthScore,
            scores.ActivityScore,
            scores.CompletionScore,
            scores.BacklogAgingScore,
            scores.MonthClosingScore,
            scores.DataQualityScore,
            metrics.Activity7Days,
            metrics.Activity30Days,
            metrics.Previous7DaysActivity,
            trendPercent,
            metrics.Registered30Days,
            metrics.Posted30Days,
            postingRate30Days,
            metrics.BacklogCount,
            metrics.Backlog7DaysPlus,
            metrics.Backlog30DaysPlus,
            metrics.OldestOpenAgeDays,
            metrics.LastActivityDate,
            metrics.PreviousMonthPostingRate,
            metrics.HistoricalPostingRate,
            metrics.Imported30Days,
            metrics.Platform30Days,
            metrics.Manual30Days,
            metrics.DuplicateDocumentLinks,
            metrics.DataQualityIssues,
            string.Join(',', warnings));
    }

    private static OmegaDashboardScores CalculateScores(OmegaDashboardMetrics metrics)
    {
        var activityScore = metrics.Activity30Days switch
        {
            0 when metrics.BacklogCount > 0 => 15,
            0 => 55,
            _ => 100
        };

        if (metrics.Previous7DaysActivity > 0 && metrics.Activity7Days < metrics.Previous7DaysActivity / 2m)
        {
            activityScore -= 25;
        }

        var completionScore = metrics.Registered30Days == 0
            ? 70
            : (int)Math.Round(Math.Min(100m, CalculateRate(metrics.Posted30Days, metrics.Registered30Days)));

        if (metrics.Registered30Days > 0 && metrics.Posted30Days == 0)
        {
            completionScore = 0;
        }

        var backlogAgingScore = 100
            - (int)Math.Min(45, metrics.Backlog7DaysPlus * 3)
            - (int)Math.Min(55, metrics.Backlog30DaysPlus * 10);

        if (metrics.OldestOpenAgeDays > 30)
        {
            backlogAgingScore -= Math.Min(30, (metrics.OldestOpenAgeDays.Value - 30) / 2);
        }

        var monthClosingScore = 100;

        if (metrics.PreviousMonthPostingRate is < 95m)
        {
            monthClosingScore -= 35;
        }

        if (metrics.HistoricalPostingRate is < 98m)
        {
            monthClosingScore -= 45;
        }

        var dataQualityScore = metrics.DataQualityIssues == 0
            ? 100
            : Math.Max(0, 100 - (int)Math.Min(100, metrics.DataQualityIssues * 10));

        activityScore = ClampScore(activityScore);
        completionScore = ClampScore(completionScore);
        backlogAgingScore = ClampScore(backlogAgingScore);
        monthClosingScore = ClampScore(monthClosingScore);
        dataQualityScore = ClampScore(dataQualityScore);

        var healthScore = ClampScore((int)Math.Round(
            activityScore * 0.20m +
            completionScore * 0.30m +
            backlogAgingScore * 0.25m +
            monthClosingScore * 0.20m +
            dataQualityScore * 0.05m));

        return new OmegaDashboardScores(
            healthScore,
            activityScore,
            completionScore,
            backlogAgingScore,
            monthClosingScore,
            dataQualityScore);
    }

    private static IReadOnlyList<string> BuildWarnings(OmegaDashboardMetrics metrics)
    {
        var warnings = new List<string>();

        if (metrics.Backlog30DaysPlus > 0)
            warnings.Add("OLD_UNPOSTED_30D");

        if (metrics.Backlog7DaysPlus > 0)
            warnings.Add("OLD_UNPOSTED_7D");

        if (metrics.Registered30Days > 0 && metrics.Posted30Days == 0)
            warnings.Add("ZERO_POSTING");

        if (metrics.Previous7DaysActivity > 0 && metrics.Activity7Days < metrics.Previous7DaysActivity / 2m)
            warnings.Add("ACTIVITY_DROP");

        if (metrics.Imported30Days + metrics.Platform30Days > 0 && metrics.Posted30Days == 0)
            warnings.Add("IMPORT_ONLY");

        if (metrics.PreviousMonthPostingRate is < 95m || metrics.HistoricalPostingRate is < 98m)
            warnings.Add("BROKEN_MONTH");

        if (metrics.DuplicateDocumentLinks > 0)
            warnings.Add("DUPLICATES");

        if (metrics.NullDateCount > 0)
            warnings.Add("NULL_DATE");

        if (metrics.OrphanLineCount > 0)
            warnings.Add("ORPHAN_LINES");

        if (metrics.OrphanPaymentCount > 0)
            warnings.Add("ORPHAN_PAYMENTS");

        if (metrics.UnknownCircuitCount > 0)
            warnings.Add("UNKNOWN_CIRCUIT");

        return warnings;
    }

    private static decimal CalculateRate(long numerator, long denominator)
    {
        return denominator == 0 ? 0 : decimal.Round(numerator * 100m / denominator, 2);
    }

    private static decimal? CalculateTrendPercent(long current, long previous)
    {
        if (previous == 0)
        {
            return current == 0 ? 0 : 100;
        }

        return decimal.Round((current - previous) * 100m / previous, 2);
    }

    private static int ClampScore(int value)
    {
        return Math.Max(0, Math.Min(100, value));
    }

    private static long GetInt64(SqlDataReader reader, string name)
    {
        var value = reader[name];
        return value is null or DBNull ? 0 : Convert.ToInt64(value);
    }

    private static int? GetNullableInt32(SqlDataReader reader, string name)
    {
        var value = reader[name];
        return value is null or DBNull ? null : Convert.ToInt32(value);
    }

    private static decimal? GetNullableDecimal(SqlDataReader reader, string name)
    {
        var value = reader[name];
        return value is null or DBNull ? null : Convert.ToDecimal(value);
    }

    private static DateTime? GetNullableDateTime(SqlDataReader reader, string name)
    {
        var value = reader[name];
        return value is null or DBNull ? null : Convert.ToDateTime(value);
    }

    private sealed record OmegaDashboardMetrics(
        long Activity7Days,
        long Activity30Days,
        long Previous7DaysActivity,
        long Registered30Days,
        long Posted30Days,
        long BacklogCount,
        long Backlog7DaysPlus,
        long Backlog30DaysPlus,
        int? OldestOpenAgeDays,
        DateTime? LastActivityDate,
        decimal? PreviousMonthPostingRate,
        decimal? HistoricalPostingRate,
        long Imported30Days,
        long Platform30Days,
        long Manual30Days,
        long DuplicateDocumentLinks,
        long NullDateCount,
        long OrphanLineCount,
        long OrphanPaymentCount,
        long UnknownCircuitCount)
    {
        public long DataQualityIssues =>
            NullDateCount + OrphanLineCount + OrphanPaymentCount + UnknownCircuitCount;
    }

    private sealed record OmegaDashboardScores(
        int HealthScore,
        int ActivityScore,
        int CompletionScore,
        int BacklogAgingScore,
        int MonthClosingScore,
        int DataQualityScore);

    private const string SnapshotSql = """
        DECLARE @From7 date = DATEADD(day, -6, @Today);
        DECLARE @From30 date = DATEADD(day, -29, @Today);
        DECLARE @Previous7From date = DATEADD(day, -13, @Today);
        DECLARE @Previous7To date = DATEADD(day, -7, @Today);
        DECLARE @CurrentMonthStart date = DATEFROMPARTS(YEAR(@Today), MONTH(@Today), 1);
        DECLARE @PreviousMonthStart date = DATEADD(month, -1, @CurrentMonthStart);
        DECLARE @PreviousMonthEnd date = DATEADD(day, -1, @CurrentMonthStart);

        WITH documents AS (
            SELECT e.C023_CisloInterneKodOkruh AS DocumentCode,
                   e.C030_CisloInterne AS InternalNumber,
                   TRY_CONVERT(date, CONCAT(
                       e.C116_RokZaevidovania, '-',
                       e.C115_MesZaevidovania, '-',
                       e.C114_DenZaevidovania
                   )) AS RegisteredDate,
                   CASE WHEN NULLIF(LTRIM(RTRIM(e.C027_KodZauctoval)), '') IS NULL
                        THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS IsPosted,
                   CASE WHEN e.C108_PREdkontovany <> 0
                        THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsPreAccounted,
                   e.C138_PovodDokladu AS OriginCode,
                   CASE WHEN NULLIF(LTRIM(RTRIM(e.C187_PlatformaId)), '') IS NULL
                        THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS HasPlatformId
            FROM dbo.T040_EUD AS e
            WHERE ISNULL(e.C001_Vymazany, 0) = 0
              AND e.C023_CisloInterneKodOkruh IN ('DF', 'OF', 'DPF', 'OPF', 'zDF', 'zOF')
        ),
        daily AS (
            SELECT
                SUM(CASE WHEN RegisteredDate BETWEEN @From7 AND @Today THEN 1 ELSE 0 END) AS Registered7Days,
                SUM(CASE WHEN IsPosted = 1 AND RegisteredDate BETWEEN @From7 AND @Today THEN 1 ELSE 0 END) AS Posted7Days,
                SUM(CASE WHEN RegisteredDate BETWEEN @From30 AND @Today THEN 1 ELSE 0 END) AS Registered30Days,
                SUM(CASE WHEN IsPosted = 1 AND RegisteredDate BETWEEN @From30 AND @Today THEN 1 ELSE 0 END) AS Posted30Days,
                SUM(CASE WHEN RegisteredDate BETWEEN @Previous7From AND @Previous7To THEN 1 ELSE 0 END) AS PreviousRegistered7Days,
                SUM(CASE WHEN IsPosted = 1 AND RegisteredDate BETWEEN @Previous7From AND @Previous7To THEN 1 ELSE 0 END) AS PreviousPosted7Days,
                SUM(CASE WHEN IsPosted = 0 THEN 1 ELSE 0 END) AS BacklogCount,
                SUM(CASE WHEN IsPosted = 0 AND RegisteredDate <= DATEADD(day, -7, @Today) THEN 1 ELSE 0 END) AS Backlog7DaysPlus,
                SUM(CASE WHEN IsPosted = 0 AND RegisteredDate <= DATEADD(day, -30, @Today) THEN 1 ELSE 0 END) AS Backlog30DaysPlus,
                MAX(CASE WHEN IsPosted = 0 THEN DATEDIFF(day, RegisteredDate, @Today) END) AS OldestOpenAgeDays,
                MAX(RegisteredDate) AS LastActivityDate,
                SUM(CASE WHEN RegisteredDate BETWEEN @From30 AND @Today AND HasPlatformId = 1 THEN 1 ELSE 0 END) AS Platform30Days,
                SUM(CASE WHEN RegisteredDate BETWEEN @From30 AND @Today AND HasPlatformId = 0 AND ISNULL(OriginCode, 0) <> 0 THEN 1 ELSE 0 END) AS Imported30Days,
                SUM(CASE WHEN RegisteredDate BETWEEN @From30 AND @Today AND HasPlatformId = 0 AND ISNULL(OriginCode, 0) = 0 THEN 1 ELSE 0 END) AS Manual30Days
            FROM documents
        ),
        previous_month AS (
            SELECT COUNT_BIG(*) AS RegisteredCount,
                   SUM(CASE WHEN IsPosted = 1 THEN 1 ELSE 0 END) AS PostedCount
            FROM documents
            WHERE RegisteredDate BETWEEN @PreviousMonthStart AND @PreviousMonthEnd
        ),
        historical_months AS (
            SELECT COUNT_BIG(*) AS RegisteredCount,
                   SUM(CASE WHEN IsPosted = 1 THEN 1 ELSE 0 END) AS PostedCount
            FROM documents
            WHERE RegisteredDate < @PreviousMonthStart
        ),
        duplicate_links AS (
            SELECT COUNT_BIG(*) AS IssueCount
            FROM dbo.T228_Faktury AS f
            WHERE f.C022_KodEvidencia IN ('OF', 'OPF')
              AND 1 < (
                  SELECT COUNT(*)
                  FROM dbo.T040_EUD AS e
                  WHERE e.C023_CisloInterneKodOkruh = f.C022_KodEvidencia
                    AND ISNULL(e.C001_Vymazany, 0) = 0
                    AND e.C030_CisloInterne = f.C030_CisloFaktury
              )
        ),
        data_quality AS (
            SELECT
                (
                    SELECT COUNT_BIG(*)
                    FROM documents
                    WHERE RegisteredDate IS NULL
                ) AS NullDateCount,
                (
                    SELECT COUNT_BIG(*)
                    FROM dbo.T041_EUD_Polozky AS p
                    LEFT JOIN dbo.T040_EUD AS h ON h.C000_ID = p.C010_IDEUD
                    WHERE h.C000_ID IS NULL
                ) AS OrphanLineCount,
                (
                    SELECT COUNT_BIG(*)
                    FROM dbo.T047_EUD_UhradyOdpocty AS p
                    LEFT JOIN dbo.T040_EUD AS h ON h.C000_ID = p.C010_IDEUD
                    WHERE h.C000_ID IS NULL
                ) AS OrphanPaymentCount,
                (
                    SELECT COUNT_BIG(*)
                    FROM dbo.T040_EUD AS e
                    LEFT JOIN dbo.T025_OkruhyUctovnychDokladov AS k
                        ON k.C021_Kod = e.C023_CisloInterneKodOkruh
                    WHERE ISNULL(e.C001_Vymazany, 0) = 0
                      AND e.C023_CisloInterneKodOkruh IN ('DF', 'OF', 'DPF', 'OPF', 'zDF', 'zOF')
                      AND k.C021_Kod IS NULL
                ) AS UnknownCircuitCount
        )
        SELECT daily.Registered7Days,
               daily.Posted7Days,
               daily.Registered30Days,
               daily.Posted30Days,
               daily.PreviousRegistered7Days,
               daily.PreviousPosted7Days,
               daily.BacklogCount,
               daily.Backlog7DaysPlus,
               daily.Backlog30DaysPlus,
               daily.OldestOpenAgeDays,
               daily.LastActivityDate,
               CASE WHEN previous_month.RegisteredCount = 0 THEN NULL
                    ELSE CONVERT(decimal(5, 2), previous_month.PostedCount * 100.0 / previous_month.RegisteredCount) END AS PreviousMonthPostingRate,
               CASE WHEN historical_months.RegisteredCount = 0 THEN NULL
                    ELSE CONVERT(decimal(5, 2), historical_months.PostedCount * 100.0 / historical_months.RegisteredCount) END AS HistoricalPostingRate,
               daily.Imported30Days,
               daily.Platform30Days,
               daily.Manual30Days,
               duplicate_links.IssueCount AS DuplicateDocumentLinks,
               data_quality.NullDateCount,
               data_quality.OrphanLineCount,
               data_quality.OrphanPaymentCount,
               data_quality.UnknownCircuitCount
        FROM daily
        CROSS JOIN previous_month
        CROSS JOIN historical_months
        CROSS JOIN duplicate_links
        CROSS JOIN data_quality
        """;
}
