using KrosExtension.Domain.Configuration;
using KrosExtension.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;
using System.Data;

namespace KrosExtension.Infrastructure.Companies;

internal static class OlympDashboardSnapshotCalculator
{
    public static async Task<OlympCompanyDashboardSnapshot?> ReadSnapshotAsync(
        DataReadServer server,
        TrackedCompany company,
        DateTime today,
        CancellationToken cancellationToken)
    {
        var connectionString = DataReadServerConnectionStringFactory.CreateDatabaseConnectionString(
            server, ProductType.Olymp, company.DatabaseName);

        if (connectionString is null)
        {
            return null;
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var activity = await ReadActivityAsync(connection, today, cancellationToken);
        var payroll = await ReadPayrollAsync(connection, today, cancellationToken);

        var scores = CalculateScores(activity, payroll, today);
        var warnings = BuildWarnings(activity, payroll, today);
        var trendPercent = CalculateTrendPercent(activity.Activity7Days, activity.Previous7DaysActivity);

        return new OlympCompanyDashboardSnapshot(
            company.DataReadServerId,
            server.DisplayName,
            company.DatabaseName,
            company.CompanyName,
            company.AccountingYear,
            DateTimeOffset.UtcNow,
            scores.HealthScore,
            scores.ActivityScore,
            scores.PayrollCurrentnessScore,
            scores.PayrollConsistencyScore,
            scores.PeriodRegularityScore,
            activity.LastActivityDate,
            payroll.LastProcessedPeriod,
            payroll.CurrentMonthProcessed,
            payroll.ProcessedPeriodsInYear,
            activity.Activity7Days,
            activity.Activity30Days,
            activity.NullStartTimeCount,
            payroll.MissingPeriodsCount,
            payroll.InconsistentPeriodsCount,
            trendPercent,
            string.Join(',', warnings));
    }

    private static OlympDashboardScores CalculateScores(
        ActivityMetrics activity,
        PayrollMetrics payroll,
        DateTime today)
    {
        var activityScore = activity.DaysSinceLastActivity switch
        {
            null => 0,
            <= 7 => 100,
            <= 14 => 80,
            <= 30 => 60,
            <= 60 => 30,
            <= 90 => 15,
            _ => 0
        };

        if (activity.NullStartTimeCount > 0)
        {
            activityScore = Math.Max(0, activityScore - 15);
        }

        var monthsBehind = payroll.MonthsBehindCurrent;
        var payrollCurrentnessScore = monthsBehind switch
        {
            0 => 100,
            <= 1 when today.Day < 15 => 85,
            <= 1 => 50,
            <= 2 => 20,
            _ => 0
        };

        var payrollConsistencyScore = payroll.TotalPeriods == 0
            ? 70
            : ClampScore((int)Math.Round(payroll.ConsistentPeriods * 100m / payroll.TotalPeriods));

        var periodRegularityScore = ClampScore(100 - payroll.MissingPeriodsCount * 20);

        activityScore = ClampScore(activityScore);
        payrollCurrentnessScore = ClampScore(payrollCurrentnessScore);

        var healthScore = ClampScore((int)Math.Round(
            activityScore * 0.25m +
            payrollCurrentnessScore * 0.35m +
            payrollConsistencyScore * 0.25m +
            periodRegularityScore * 0.15m));

        return new OlympDashboardScores(
            healthScore,
            activityScore,
            payrollCurrentnessScore,
            payrollConsistencyScore,
            periodRegularityScore);
    }

    private static IReadOnlyList<string> BuildWarnings(
        ActivityMetrics activity,
        PayrollMetrics payroll,
        DateTime today)
    {
        var warnings = new List<string>();

        if (!payroll.CurrentMonthProcessed && today.Day >= 15)
            warnings.Add("CURRENT_MONTH_MISSING");

        if (payroll.MonthsBehindCurrent >= 2)
            warnings.Add("PAYROLL_MONTHS_BEHIND");

        if (activity.DaysSinceLastActivity is null or >= 30)
            warnings.Add("INACTIVE_30D");
        else if (activity.DaysSinceLastActivity >= 14)
            warnings.Add("INACTIVE_14D");

        if (payroll.MissingPeriodsCount > 0)
            warnings.Add("MISSING_PERIODS");

        if (payroll.InconsistentPeriodsCount > 0)
            warnings.Add("PAYROLL_INCONSISTENCY");

        if (activity.NullStartTimeCount > 0)
            warnings.Add("NULL_STARTTIME");

        var expectedWeeklyAvg = activity.Activity30Days / 4m;
        if (expectedWeeklyAvg > 0 && activity.Activity7Days < expectedWeeklyAvg * 0.30m)
            warnings.Add("LOW_ACTIVITY");

        return warnings;
    }

    private static async Task<ActivityMetrics> ReadActivityAsync(
        SqlConnection connection,
        DateTime today,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = ActivitySql;
        command.Parameters.Add(new SqlParameter("@Today", SqlDbType.Date) { Value = today.Date });
        command.Parameters.Add(new SqlParameter("@From7", SqlDbType.Date) { Value = today.AddDays(-6).Date });
        command.Parameters.Add(new SqlParameter("@From30", SqlDbType.Date) { Value = today.AddDays(-29).Date });
        command.Parameters.Add(new SqlParameter("@Prev7From", SqlDbType.Date) { Value = today.AddDays(-13).Date });
        command.Parameters.Add(new SqlParameter("@Prev7To", SqlDbType.Date) { Value = today.AddDays(-7).Date });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return new ActivityMetrics(null, null, 0, 0, 0, 0);
        }

        var lastActivityDate = GetNullableDateTime(reader, "LastActivityDate");
        var daysSince = lastActivityDate.HasValue
            ? (int)(today.Date - lastActivityDate.Value.Date).TotalDays
            : (int?)null;

        return new ActivityMetrics(
            daysSince,
            lastActivityDate,
            GetInt64(reader, "Activity7Days"),
            GetInt64(reader, "Activity30Days"),
            GetInt64(reader, "Previous7DaysActivity"),
            GetInt64(reader, "NullStartTimeCount"));
    }

    private static async Task<PayrollMetrics> ReadPayrollAsync(
        SqlConnection connection,
        DateTime today,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = PayrollSql;
        command.Parameters.Add(new SqlParameter("@Today", SqlDbType.Date) { Value = today.Date });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return new PayrollMetrics(null, false, 0, 99, 0, 0, 0);
        }

        var lastProcessedPeriod = GetNullableString(reader, "LastProcessedPeriod");
        var currentMonthProcessed = GetBoolean(reader, "CurrentMonthProcessed");
        var processedPeriodsInYear = (int)GetInt64(reader, "ProcessedPeriodsInYear");
        var totalPeriods = (int)GetInt64(reader, "TotalPeriods");
        var consistentPeriods = (int)GetInt64(reader, "ConsistentPeriods");
        var missingPeriodsCount = (int)GetInt64(reader, "MissingPeriodsCount");
        var monthsBehind = ComputeMonthsBehind(lastProcessedPeriod, today);

        return new PayrollMetrics(
            lastProcessedPeriod,
            currentMonthProcessed,
            processedPeriodsInYear,
            monthsBehind,
            totalPeriods,
            consistentPeriods,
            missingPeriodsCount);
    }

    private static int ComputeMonthsBehind(string? lastProcessedPeriod, DateTime today)
    {
        if (lastProcessedPeriod is null || !int.TryParse(lastProcessedPeriod, out var period))
        {
            return 99;
        }

        var lastYear = period / 100;
        var lastMonth = period % 100;
        var currentTotal = today.Year * 12 + today.Month;
        var lastTotal = lastYear * 12 + lastMonth;
        return Math.Max(0, currentTotal - lastTotal);
    }

    private static decimal? CalculateTrendPercent(long current, long previous)
    {
        if (previous == 0)
        {
            return current == 0 ? 0 : 100;
        }

        return decimal.Round((current - previous) * 100m / previous, 2);
    }

    private static int ClampScore(int value) => Math.Max(0, Math.Min(100, value));

    private static long GetInt64(SqlDataReader reader, string name)
    {
        var value = reader[name];
        return value is null or DBNull ? 0 : Convert.ToInt64(value);
    }

    private static bool GetBoolean(SqlDataReader reader, string name)
    {
        var value = reader[name];
        return value is not null and not DBNull && Convert.ToBoolean(value);
    }

    private static DateTime? GetNullableDateTime(SqlDataReader reader, string name)
    {
        var value = reader[name];
        return value is null or DBNull ? null : Convert.ToDateTime(value);
    }

    private static string? GetNullableString(SqlDataReader reader, string name)
    {
        var value = reader[name];
        return value is null or DBNull ? null : value.ToString();
    }

    private sealed record ActivityMetrics(
        int? DaysSinceLastActivity,
        DateTime? LastActivityDate,
        long Activity7Days,
        long Activity30Days,
        long Previous7DaysActivity,
        long NullStartTimeCount);

    private sealed record PayrollMetrics(
        string? LastProcessedPeriod,
        bool CurrentMonthProcessed,
        int ProcessedPeriodsInYear,
        int MonthsBehindCurrent,
        int TotalPeriods,
        int ConsistentPeriods,
        int MissingPeriodsCount)
    {
        public int InconsistentPeriodsCount => TotalPeriods - ConsistentPeriods;
    }

    private sealed record OlympDashboardScores(
        int HealthScore,
        int ActivityScore,
        int PayrollCurrentnessScore,
        int PayrollConsistencyScore,
        int PeriodRegularityScore);

    private const string ActivitySql = """
        SELECT
            COUNT(CASE WHEN CAST(StartTime AS date) BETWEEN @From7 AND @Today THEN 1 END) AS Activity7Days,
            COUNT(CASE WHEN CAST(StartTime AS date) BETWEEN @From30 AND @Today THEN 1 END) AS Activity30Days,
            COUNT(CASE WHEN CAST(StartTime AS date) BETWEEN @Prev7From AND @Prev7To THEN 1 END) AS Previous7DaysActivity,
            COUNT(CASE WHEN StartTime IS NULL THEN 1 END) AS NullStartTimeCount,
            MAX(CAST(StartTime AS date)) AS LastActivityDate
        FROM dbo.activityLog
        """;

    private const string PayrollSql = """
        DECLARE @CurrentPeriod int = YEAR(@Today) * 100 + MONTH(@Today);
        DECLARE @YearStart   int = YEAR(@Today) * 100 + 1;

        WITH all_periods AS (
            SELECT DISTINCT ID_obdobie FROM dbo.pracVycMzda
            UNION
            SELECT DISTINCT ID_obdobie FROM dbo.pracVycBase
            UNION
            SELECT DISTINCT ID_obdobie FROM dbo.pracVycOdvod
        ),
        period_consistency AS (
            SELECT
                p.ID_obdobie,
                CASE WHEN m.ID_obdobie IS NOT NULL THEN 1 ELSE 0 END AS HasMzda,
                CASE WHEN b.ID_obdobie IS NOT NULL THEN 1 ELSE 0 END AS HasBase,
                CASE WHEN o.ID_obdobie IS NOT NULL THEN 1 ELSE 0 END AS HasOdvod
            FROM all_periods p
            LEFT JOIN (SELECT DISTINCT ID_obdobie FROM dbo.pracVycMzda) m ON m.ID_obdobie = p.ID_obdobie
            LEFT JOIN (SELECT DISTINCT ID_obdobie FROM dbo.pracVycBase) b ON b.ID_obdobie = p.ID_obdobie
            LEFT JOIN (SELECT DISTINCT ID_obdobie FROM dbo.pracVycOdvod) o ON o.ID_obdobie = p.ID_obdobie
        ),
        year_periods AS (
            SELECT * FROM period_consistency
            WHERE ID_obdobie >= @YearStart AND ID_obdobie <= @CurrentPeriod
        )
        SELECT
            CAST(MAX(p.ID_obdobie) AS varchar(10)) AS LastProcessedPeriod,
            CAST(CASE WHEN MAX(p.ID_obdobie) >= @CurrentPeriod THEN 1 ELSE 0 END AS bit) AS CurrentMonthProcessed,
            COUNT(*) AS ProcessedPeriodsInYear,
            COUNT(*) AS TotalPeriods,
            SUM(CASE WHEN HasMzda = 1 AND HasBase = 1 AND HasOdvod = 1 THEN 1 ELSE 0 END) AS ConsistentPeriods,
            CASE
                WHEN MAX(p.ID_obdobie) IS NULL THEN 0
                ELSE (MAX(p.ID_obdobie) % 100) - COUNT(*)
            END AS MissingPeriodsCount
        FROM year_periods p
        """;
}
