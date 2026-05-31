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
            GetInt64(reader, "StornoCount"),
            GetInt64(reader, "OriginalCount"),
            GetInt64(reader, "PreAccountedStuck"),
            GetInt64(reader, "NullDateCount"),
            GetInt64(reader, "OrphanLineCount"),
            GetInt64(reader, "OrphanPaymentCount"),
            GetInt64(reader, "UnknownCircuitCount"),
            GetNullableInt32(reader, "PostingLagDfAvgDays"),
            GetNullableInt32(reader, "PostingLagDfMaxDays"),
            GetInt64(reader, "PostingLagDfCount"),
            GetNullableInt32(reader, "PostingLagOfAvgDays"),
            GetNullableInt32(reader, "PostingLagOfMaxDays"),
            GetInt64(reader, "PostingLagOfCount"),
            GetNullableInt32(reader, "PostingLagPdAvgDays"),
            GetNullableInt32(reader, "PostingLagPdMaxDays"),
            GetInt64(reader, "PostingLagPdCount"),
            GetNullableInt32(reader, "PostingLagBvAvgDays"),
            GetNullableInt32(reader, "PostingLagBvMaxDays"),
            GetInt64(reader, "PostingLagBvCount"),
            GetNullableInt32(reader, "LateEntryDfAvgDays"),
            GetNullableInt32(reader, "LateEntryDfMaxDays"),
            GetInt64(reader, "LateEntryDfCount"),
            GetNullableInt32(reader, "LateEntryOfAvgDays"),
            GetNullableInt32(reader, "LateEntryOfMaxDays"),
            GetInt64(reader, "LateEntryOfCount"),
            GetNullableInt32(reader, "LateEntryPdAvgDays"),
            GetNullableInt32(reader, "LateEntryPdMaxDays"),
            GetInt64(reader, "LateEntryPdCount"));

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
            scores.TimelinessScore,
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
            metrics.StornoRate30Days,
            metrics.PreAccountedStuck,
            metrics.DataQualityIssues,
            metrics.LateEntryDfAvgDays,
            metrics.LateEntryDfMaxDays,
            metrics.LateEntryDfCount,
            metrics.LateEntryOfAvgDays,
            metrics.LateEntryOfMaxDays,
            metrics.LateEntryOfCount,
            metrics.LateEntryPdAvgDays,
            metrics.LateEntryPdMaxDays,
            metrics.LateEntryPdCount,
            metrics.PostingLagDfAvgDays,
            metrics.PostingLagDfMaxDays,
            metrics.PostingLagDfCount,
            metrics.PostingLagOfAvgDays,
            metrics.PostingLagOfMaxDays,
            metrics.PostingLagOfCount,
            metrics.PostingLagPdAvgDays,
            metrics.PostingLagPdMaxDays,
            metrics.PostingLagPdCount,
            metrics.PostingLagBvAvgDays,
            metrics.PostingLagBvMaxDays,
            metrics.PostingLagBvCount,
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

        var maxPostingLagAvg = new[] { metrics.PostingLagDfAvgDays, metrics.PostingLagOfAvgDays, metrics.PostingLagPdAvgDays, metrics.PostingLagBvAvgDays }
            .Where(x => x.HasValue).Select(x => x!.Value).DefaultIfEmpty(0).Max();
        var maxLateEntryAvg = new[] { metrics.LateEntryDfAvgDays, metrics.LateEntryOfAvgDays, metrics.LateEntryPdAvgDays }
            .Where(x => x.HasValue).Select(x => x!.Value).DefaultIfEmpty(0).Max();
        var timelinessInput = Math.Max(maxPostingLagAvg, maxLateEntryAvg);
        var timelinessScore = timelinessInput <= 13
            ? 100
            : timelinessInput > 60
                ? 0
                : 100 - (int)Math.Round((timelinessInput - 13) * 100.0 / 47);

        activityScore = ClampScore(activityScore);
        completionScore = ClampScore(completionScore);
        backlogAgingScore = ClampScore(backlogAgingScore);
        monthClosingScore = ClampScore(monthClosingScore);
        dataQualityScore = ClampScore(dataQualityScore);
        timelinessScore = ClampScore(timelinessScore);

        var healthScore = ClampScore((int)Math.Round(
            activityScore     * 0.17m +
            completionScore   * 0.25m +
            backlogAgingScore * 0.23m +
            monthClosingScore * 0.15m +
            dataQualityScore  * 0.05m +
            timelinessScore   * 0.15m));

        return new OmegaDashboardScores(
            healthScore,
            activityScore,
            completionScore,
            backlogAgingScore,
            monthClosingScore,
            dataQualityScore,
            timelinessScore);
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

        if (metrics.StornoRate30Days > 10m)
            warnings.Add("HIGH_STORNO_RATE");

        if (metrics.PreAccountedStuck > 0)
            warnings.Add("STUCK_PREACCOUNTED");

        if (metrics.NullDateCount > 0)
            warnings.Add("NULL_DATE");

        if (metrics.OrphanLineCount > 0)
            warnings.Add("ORPHAN_LINES");

        if (metrics.OrphanPaymentCount > 0)
            warnings.Add("ORPHAN_PAYMENTS");

        if (metrics.UnknownCircuitCount > 0)
            warnings.Add("UNKNOWN_CIRCUIT");

        var maxPostingLagAvg = new[] { metrics.PostingLagDfAvgDays, metrics.PostingLagOfAvgDays, metrics.PostingLagPdAvgDays, metrics.PostingLagBvAvgDays }
            .Where(x => x.HasValue).Select(x => x!.Value).DefaultIfEmpty(0).Max();

        if (maxPostingLagAvg > 30)
            warnings.Add("POSTING_LAG_CRIT");
        else if (maxPostingLagAvg > 14)
            warnings.Add("POSTING_LAG");

        var maxLateEntryAvg = new[] { metrics.LateEntryDfAvgDays, metrics.LateEntryOfAvgDays, metrics.LateEntryPdAvgDays }
            .Where(x => x.HasValue).Select(x => x!.Value).DefaultIfEmpty(0).Max();

        if (maxLateEntryAvg > 30)
            warnings.Add("LATE_ENTRY_CRIT");
        else if (maxLateEntryAvg > 14)
            warnings.Add("LATE_ENTRY");

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
        long StornoCount,
        long OriginalCount,
        long PreAccountedStuck,
        long NullDateCount,
        long OrphanLineCount,
        long OrphanPaymentCount,
        long UnknownCircuitCount,
        int? PostingLagDfAvgDays,
        int? PostingLagDfMaxDays,
        long PostingLagDfCount,
        int? PostingLagOfAvgDays,
        int? PostingLagOfMaxDays,
        long PostingLagOfCount,
        int? PostingLagPdAvgDays,
        int? PostingLagPdMaxDays,
        long PostingLagPdCount,
        int? PostingLagBvAvgDays,
        int? PostingLagBvMaxDays,
        long PostingLagBvCount,
        int? LateEntryDfAvgDays,
        int? LateEntryDfMaxDays,
        long LateEntryDfCount,
        int? LateEntryOfAvgDays,
        int? LateEntryOfMaxDays,
        long LateEntryOfCount,
        int? LateEntryPdAvgDays,
        int? LateEntryPdMaxDays,
        long LateEntryPdCount)
    {
        public long DataQualityIssues =>
            NullDateCount + OrphanLineCount + OrphanPaymentCount + UnknownCircuitCount;

        public decimal? StornoRate30Days =>
            OriginalCount == 0 ? null : decimal.Round(StornoCount * 100m / OriginalCount, 2);
    }

    private sealed record OmegaDashboardScores(
        int HealthScore,
        int ActivityScore,
        int CompletionScore,
        int BacklogAgingScore,
        int MonthClosingScore,
        int DataQualityScore,
        int TimelinessScore);

    private const string SnapshotSql = """
        DECLARE @From7 date = DATEADD(day, -6, @Today);
        DECLARE @From30 date = DATEADD(day, -29, @Today);
        DECLARE @From90 date = DATEADD(day, -89, @Today);
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
        storno AS (
            SELECT
                SUM(CASE WHEN DocumentCode IN ('zDF', 'zOF') THEN 1 ELSE 0 END) AS StornoCount,
                SUM(CASE WHEN DocumentCode IN ('DF', 'OF', 'DPF', 'OPF') THEN 1 ELSE 0 END) AS OriginalCount
            FROM documents
            WHERE RegisteredDate BETWEEN @From30 AND @Today
        ),
        preaccounted_stuck AS (
            SELECT COUNT_BIG(*) AS StuckCount
            FROM documents
            WHERE IsPreAccounted = 1
              AND IsPosted = 0
        ),
        posting_lag AS (
            SELECT
                AVG(CASE WHEN DocumentCode = 'DF' AND PostingLagDays BETWEEN 0 AND 3650 THEN PostingLagDays END) AS PostingLagDfAvgDays,
                MAX(CASE WHEN DocumentCode = 'DF' AND PostingLagDays >= 0 THEN PostingLagDays END) AS PostingLagDfMaxDays,
                COUNT(CASE WHEN DocumentCode = 'DF' AND PostingLagDays >= 0 THEN 1 END) AS PostingLagDfCount,
                AVG(CASE WHEN DocumentCode = 'OF' AND PostingLagDays BETWEEN 0 AND 3650 THEN PostingLagDays END) AS PostingLagOfAvgDays,
                MAX(CASE WHEN DocumentCode = 'OF' AND PostingLagDays >= 0 THEN PostingLagDays END) AS PostingLagOfMaxDays,
                COUNT(CASE WHEN DocumentCode = 'OF' AND PostingLagDays >= 0 THEN 1 END) AS PostingLagOfCount,
                AVG(CASE WHEN DocumentCode = 'PD' AND PostingLagDays BETWEEN 0 AND 3650 THEN PostingLagDays END) AS PostingLagPdAvgDays,
                MAX(CASE WHEN DocumentCode = 'PD' AND PostingLagDays >= 0 THEN PostingLagDays END) AS PostingLagPdMaxDays,
                COUNT(CASE WHEN DocumentCode = 'PD' AND PostingLagDays >= 0 THEN 1 END) AS PostingLagPdCount,
                AVG(CASE WHEN DocumentCode = 'BV' AND PostingLagDays BETWEEN 0 AND 3650 THEN PostingLagDays END) AS PostingLagBvAvgDays,
                MAX(CASE WHEN DocumentCode = 'BV' AND PostingLagDays >= 0 THEN PostingLagDays END) AS PostingLagBvMaxDays,
                COUNT(CASE WHEN DocumentCode = 'BV' AND PostingLagDays >= 0 THEN 1 END) AS PostingLagBvCount
            FROM (
                SELECT
                    e.C023_CisloInterneKodOkruh AS DocumentCode,
                    DATEDIFF(day,
                        TRY_CONVERT(date, CONCAT(e.C116_RokZaevidovania, '-', e.C115_MesZaevidovania, '-', e.C114_DenZaevidovania)),
                        TRY_CONVERT(date, CONCAT(e.C119_RokPoslednejOpravy, '-', e.C118_MesPoslednejOpravy, '-', e.C117_DenPoslednejOpravy))
                    ) AS PostingLagDays
                FROM dbo.T040_EUD AS e
                WHERE ISNULL(e.C001_Vymazany, 0) = 0
                  AND e.C023_CisloInterneKodOkruh IN ('DF', 'OF', 'PD', 'BV')
                  AND NULLIF(LTRIM(RTRIM(e.C027_KodZauctoval)), '') IS NOT NULL
                  AND TRY_CONVERT(date, CONCAT(e.C119_RokPoslednejOpravy, '-', e.C118_MesPoslednejOpravy, '-', e.C117_DenPoslednejOpravy))
                      BETWEEN @From90 AND @Today
            ) AS src
        ),
        late_entry AS (
            SELECT
                AVG(CASE WHEN DocumentCode = 'DF' AND LateEntryDays BETWEEN 0 AND 3650 THEN LateEntryDays END) AS LateEntryDfAvgDays,
                MAX(CASE WHEN DocumentCode = 'DF' AND LateEntryDays >= 0 THEN LateEntryDays END) AS LateEntryDfMaxDays,
                COUNT(CASE WHEN DocumentCode = 'DF' AND LateEntryDays >= 0 THEN 1 END) AS LateEntryDfCount,
                AVG(CASE WHEN DocumentCode = 'OF' AND LateEntryDays BETWEEN 0 AND 3650 THEN LateEntryDays END) AS LateEntryOfAvgDays,
                MAX(CASE WHEN DocumentCode = 'OF' AND LateEntryDays >= 0 THEN LateEntryDays END) AS LateEntryOfMaxDays,
                COUNT(CASE WHEN DocumentCode = 'OF' AND LateEntryDays >= 0 THEN 1 END) AS LateEntryOfCount,
                AVG(CASE WHEN DocumentCode = 'PD' AND LateEntryDays BETWEEN 0 AND 3650 THEN LateEntryDays END) AS LateEntryPdAvgDays,
                MAX(CASE WHEN DocumentCode = 'PD' AND LateEntryDays >= 0 THEN LateEntryDays END) AS LateEntryPdMaxDays,
                COUNT(CASE WHEN DocumentCode = 'PD' AND LateEntryDays >= 0 THEN 1 END) AS LateEntryPdCount
            FROM (
                SELECT
                    e.C023_CisloInterneKodOkruh AS DocumentCode,
                    DATEDIFF(day,
                        TRY_CONVERT(date, CONCAT(e.C062_RokVystavenia, '-', e.C061_MesVystavenia, '-', e.C060_DenVystavenia)),
                        TRY_CONVERT(date, CONCAT(e.C116_RokZaevidovania, '-', e.C115_MesZaevidovania, '-', e.C114_DenZaevidovania))
                    ) AS LateEntryDays
                FROM dbo.T040_EUD AS e
                WHERE ISNULL(e.C001_Vymazany, 0) = 0
                  AND e.C023_CisloInterneKodOkruh IN ('DF', 'OF', 'PD')
                  AND TRY_CONVERT(date, CONCAT(e.C116_RokZaevidovania, '-', e.C115_MesZaevidovania, '-', e.C114_DenZaevidovania))
                      BETWEEN @From90 AND @Today
            ) AS src
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
               storno.StornoCount,
               storno.OriginalCount,
               preaccounted_stuck.StuckCount AS PreAccountedStuck,
               data_quality.NullDateCount,
               data_quality.OrphanLineCount,
               data_quality.OrphanPaymentCount,
               data_quality.UnknownCircuitCount,
               posting_lag.PostingLagDfAvgDays,
               posting_lag.PostingLagDfMaxDays,
               posting_lag.PostingLagDfCount,
               posting_lag.PostingLagOfAvgDays,
               posting_lag.PostingLagOfMaxDays,
               posting_lag.PostingLagOfCount,
               posting_lag.PostingLagPdAvgDays,
               posting_lag.PostingLagPdMaxDays,
               posting_lag.PostingLagPdCount,
               posting_lag.PostingLagBvAvgDays,
               posting_lag.PostingLagBvMaxDays,
               posting_lag.PostingLagBvCount,
               late_entry.LateEntryDfAvgDays,
               late_entry.LateEntryDfMaxDays,
               late_entry.LateEntryDfCount,
               late_entry.LateEntryOfAvgDays,
               late_entry.LateEntryOfMaxDays,
               late_entry.LateEntryOfCount,
               late_entry.LateEntryPdAvgDays,
               late_entry.LateEntryPdMaxDays,
               late_entry.LateEntryPdCount
        FROM daily
        CROSS JOIN previous_month
        CROSS JOIN historical_months
        CROSS JOIN storno
        CROSS JOIN preaccounted_stuck
        CROSS JOIN data_quality
        CROSS JOIN posting_lag
        CROSS JOIN late_entry
        """;
}
