using KrosExtension.Domain.Common;

namespace KrosExtension.Domain.Configuration;

public sealed class OlympCompanyDashboardSnapshot : Entity
{
    private OlympCompanyDashboardSnapshot()
    {
    }

    public OlympCompanyDashboardSnapshot(
        Guid dataReadServerId,
        string serverDisplayName,
        string databaseName,
        string companyName,
        int? accountingYear,
        DateTimeOffset snapshotAt,
        int healthScore,
        int activityScore,
        int payrollCurrentnessScore,
        int payrollConsistencyScore,
        int periodRegularityScore,
        DateTime? lastActivityDate,
        string? lastProcessedPeriod,
        bool currentMonthProcessed,
        int processedPeriodsInYear,
        long activity7Days,
        long activity30Days,
        long nullStartTimeCount,
        int missingPeriodsCount,
        int inconsistentPeriodsCount,
        decimal? trendPercent,
        string warningFlags)
    {
        DataReadServerId = dataReadServerId;
        ServerDisplayName = serverDisplayName;
        DatabaseName = databaseName;
        CompanyName = companyName;
        AccountingYear = accountingYear;
        SnapshotAt = snapshotAt;
        HealthScore = healthScore;
        ActivityScore = activityScore;
        PayrollCurrentnessScore = payrollCurrentnessScore;
        PayrollConsistencyScore = payrollConsistencyScore;
        PeriodRegularityScore = periodRegularityScore;
        LastActivityDate = lastActivityDate;
        LastProcessedPeriod = lastProcessedPeriod;
        CurrentMonthProcessed = currentMonthProcessed;
        ProcessedPeriodsInYear = processedPeriodsInYear;
        Activity7Days = activity7Days;
        Activity30Days = activity30Days;
        NullStartTimeCount = nullStartTimeCount;
        MissingPeriodsCount = missingPeriodsCount;
        InconsistentPeriodsCount = inconsistentPeriodsCount;
        TrendPercent = trendPercent;
        WarningFlags = warningFlags;
    }

    public Guid DataReadServerId { get; private set; }
    public string ServerDisplayName { get; private set; } = string.Empty;
    public string DatabaseName { get; private set; } = string.Empty;
    public string CompanyName { get; private set; } = string.Empty;
    public int? AccountingYear { get; private set; }
    public DateTimeOffset SnapshotAt { get; private set; }
    public int HealthScore { get; private set; }
    public int ActivityScore { get; private set; }
    public int PayrollCurrentnessScore { get; private set; }
    public int PayrollConsistencyScore { get; private set; }
    public int PeriodRegularityScore { get; private set; }
    public DateTime? LastActivityDate { get; private set; }
    public string? LastProcessedPeriod { get; private set; }
    public bool CurrentMonthProcessed { get; private set; }
    public int ProcessedPeriodsInYear { get; private set; }
    public long Activity7Days { get; private set; }
    public long Activity30Days { get; private set; }
    public long NullStartTimeCount { get; private set; }
    public int MissingPeriodsCount { get; private set; }
    public int InconsistentPeriodsCount { get; private set; }
    public decimal? TrendPercent { get; private set; }
    public string WarningFlags { get; private set; } = string.Empty;
}
