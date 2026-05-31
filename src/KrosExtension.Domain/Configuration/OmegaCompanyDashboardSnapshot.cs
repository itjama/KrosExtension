using KrosExtension.Domain.Common;

namespace KrosExtension.Domain.Configuration;

public sealed class OmegaCompanyDashboardSnapshot : Entity
{
    private OmegaCompanyDashboardSnapshot()
    {
    }

    public OmegaCompanyDashboardSnapshot(
        Guid dataReadServerId,
        string serverDisplayName,
        string databaseName,
        string companyName,
        int? accountingYear,
        DateTimeOffset snapshotAt,
        int healthScore,
        int activityScore,
        int completionScore,
        int backlogAgingScore,
        int monthClosingScore,
        int dataQualityScore,
        int timelinessScore,
        long activity7Days,
        long activity30Days,
        long previous7DaysActivity,
        decimal? trendPercent,
        long registered30Days,
        long posted30Days,
        decimal postingRate30Days,
        long backlogCount,
        long backlog7DaysPlus,
        long backlog30DaysPlus,
        int? oldestOpenAgeDays,
        DateTime? lastActivityDate,
        decimal? previousMonthPostingRate,
        decimal? historicalPostingRate,
        long imported30Days,
        long platform30Days,
        long manual30Days,
        decimal? stornoRate30Days,
        long preAccountedStuck,
        long dataQualityIssues,
        int? lateEntryDfAvgDays,
        int? lateEntryDfMaxDays,
        long lateEntryDfCount,
        int? lateEntryOfAvgDays,
        int? lateEntryOfMaxDays,
        long lateEntryOfCount,
        int? lateEntryPdAvgDays,
        int? lateEntryPdMaxDays,
        long lateEntryPdCount,
        int? postingLagDfAvgDays,
        int? postingLagDfMaxDays,
        long postingLagDfCount,
        int? postingLagOfAvgDays,
        int? postingLagOfMaxDays,
        long postingLagOfCount,
        int? postingLagPdAvgDays,
        int? postingLagPdMaxDays,
        long postingLagPdCount,
        int? postingLagBvAvgDays,
        int? postingLagBvMaxDays,
        long postingLagBvCount,
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
        CompletionScore = completionScore;
        BacklogAgingScore = backlogAgingScore;
        MonthClosingScore = monthClosingScore;
        DataQualityScore = dataQualityScore;
        TimelinessScore = timelinessScore;
        Activity7Days = activity7Days;
        Activity30Days = activity30Days;
        Previous7DaysActivity = previous7DaysActivity;
        TrendPercent = trendPercent;
        Registered30Days = registered30Days;
        Posted30Days = posted30Days;
        PostingRate30Days = postingRate30Days;
        BacklogCount = backlogCount;
        Backlog7DaysPlus = backlog7DaysPlus;
        Backlog30DaysPlus = backlog30DaysPlus;
        OldestOpenAgeDays = oldestOpenAgeDays;
        LastActivityDate = lastActivityDate;
        PreviousMonthPostingRate = previousMonthPostingRate;
        HistoricalPostingRate = historicalPostingRate;
        Imported30Days = imported30Days;
        Platform30Days = platform30Days;
        Manual30Days = manual30Days;
        StornoRate30Days = stornoRate30Days;
        PreAccountedStuck = preAccountedStuck;
        DataQualityIssues = dataQualityIssues;
        LateEntryDfAvgDays = lateEntryDfAvgDays;
        LateEntryDfMaxDays = lateEntryDfMaxDays;
        LateEntryDfCount = lateEntryDfCount;
        LateEntryOfAvgDays = lateEntryOfAvgDays;
        LateEntryOfMaxDays = lateEntryOfMaxDays;
        LateEntryOfCount = lateEntryOfCount;
        LateEntryPdAvgDays = lateEntryPdAvgDays;
        LateEntryPdMaxDays = lateEntryPdMaxDays;
        LateEntryPdCount = lateEntryPdCount;
        PostingLagDfAvgDays = postingLagDfAvgDays;
        PostingLagDfMaxDays = postingLagDfMaxDays;
        PostingLagDfCount = postingLagDfCount;
        PostingLagOfAvgDays = postingLagOfAvgDays;
        PostingLagOfMaxDays = postingLagOfMaxDays;
        PostingLagOfCount = postingLagOfCount;
        PostingLagPdAvgDays = postingLagPdAvgDays;
        PostingLagPdMaxDays = postingLagPdMaxDays;
        PostingLagPdCount = postingLagPdCount;
        PostingLagBvAvgDays = postingLagBvAvgDays;
        PostingLagBvMaxDays = postingLagBvMaxDays;
        PostingLagBvCount = postingLagBvCount;
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

    public int CompletionScore { get; private set; }

    public int BacklogAgingScore { get; private set; }

    public int MonthClosingScore { get; private set; }

    public int DataQualityScore { get; private set; }

    public int TimelinessScore { get; private set; }

    public long Activity7Days { get; private set; }

    public long Activity30Days { get; private set; }

    public long Previous7DaysActivity { get; private set; }

    public decimal? TrendPercent { get; private set; }

    public long Registered30Days { get; private set; }

    public long Posted30Days { get; private set; }

    public decimal PostingRate30Days { get; private set; }

    public long BacklogCount { get; private set; }

    public long Backlog7DaysPlus { get; private set; }

    public long Backlog30DaysPlus { get; private set; }

    public int? OldestOpenAgeDays { get; private set; }

    public DateTime? LastActivityDate { get; private set; }

    public decimal? PreviousMonthPostingRate { get; private set; }

    public decimal? HistoricalPostingRate { get; private set; }

    public long Imported30Days { get; private set; }

    public long Platform30Days { get; private set; }

    public long Manual30Days { get; private set; }

    public decimal? StornoRate30Days { get; private set; }

    public long PreAccountedStuck { get; private set; }

    public long DataQualityIssues { get; private set; }

    public int? LateEntryDfAvgDays { get; private set; }

    public int? LateEntryDfMaxDays { get; private set; }

    public long LateEntryDfCount { get; private set; }

    public int? LateEntryOfAvgDays { get; private set; }

    public int? LateEntryOfMaxDays { get; private set; }

    public long LateEntryOfCount { get; private set; }

    public int? LateEntryPdAvgDays { get; private set; }

    public int? LateEntryPdMaxDays { get; private set; }

    public long LateEntryPdCount { get; private set; }

    public int? PostingLagDfAvgDays { get; private set; }

    public int? PostingLagDfMaxDays { get; private set; }

    public long PostingLagDfCount { get; private set; }

    public int? PostingLagOfAvgDays { get; private set; }

    public int? PostingLagOfMaxDays { get; private set; }

    public long PostingLagOfCount { get; private set; }

    public int? PostingLagPdAvgDays { get; private set; }

    public int? PostingLagPdMaxDays { get; private set; }

    public long PostingLagPdCount { get; private set; }

    public int? PostingLagBvAvgDays { get; private set; }

    public int? PostingLagBvMaxDays { get; private set; }

    public long PostingLagBvCount { get; private set; }

    public string WarningFlags { get; private set; } = string.Empty;
}
