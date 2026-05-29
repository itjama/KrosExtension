namespace KrosExtension.Application.Companies;

public sealed record OlympDashboardCompany(
    Guid DataReadServerId,
    string ServerDisplayName,
    string DatabaseName,
    string CompanyName,
    int? AccountingYear,
    DateTimeOffset SnapshotAt,
    int HealthScore,
    int ActivityScore,
    int PayrollCurrentnessScore,
    int PayrollConsistencyScore,
    int PeriodRegularityScore,
    DateTime? LastActivityDate,
    string? LastProcessedPeriod,
    bool CurrentMonthProcessed,
    int ProcessedPeriodsInYear,
    long Activity7Days,
    long Activity30Days,
    decimal? TrendPercent,
    IReadOnlyList<string> WarningFlags);
