using KrosExtension.Application.Companies;
using KrosExtension.Domain.Configuration;
using KrosExtension.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KrosExtension.Infrastructure.Companies;

public sealed class OmegaDashboardReader(ApplicationDbContext dbContext) : IOmegaDashboardReader
{
    public async Task<IReadOnlyList<OmegaDashboardCompany>> GetCompaniesAsync(
        CancellationToken cancellationToken = default)
    {
        var trackedKeys = await dbContext.TrackedCompanies
            .AsNoTracking()
            .Where(tc => tc.ProductType == ProductType.Omega && tc.IsTracked)
            .Select(tc => new { tc.DataReadServerId, tc.DatabaseName })
            .ToListAsync(cancellationToken);

        var trackedSet = trackedKeys
            .Select(k => (k.DataReadServerId, k.DatabaseName))
            .ToHashSet();

        var snapshots = await dbContext.OmegaCompanyDashboardSnapshots
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var latestSnapshots = snapshots
            .Where(snapshot => trackedSet.Contains((snapshot.DataReadServerId, snapshot.DatabaseName)))
            .GroupBy(snapshot => new
            {
                snapshot.DataReadServerId,
                snapshot.DatabaseName
            })
            .Select(group => group
                .OrderByDescending(snapshot => snapshot.SnapshotAt)
                .First())
            .OrderBy(snapshot => snapshot.HealthScore)
            .ThenByDescending(snapshot => snapshot.Backlog30DaysPlus)
            .ThenByDescending(snapshot => snapshot.BacklogCount)
            .ThenBy(snapshot => snapshot.LastActivityDate)
            .ToList();

        return latestSnapshots.Select(snapshot => new OmegaDashboardCompany(
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
            snapshot.Activity7Days,
            snapshot.Activity30Days,
            snapshot.Previous7DaysActivity,
            snapshot.TrendPercent,
            snapshot.Registered30Days,
            snapshot.Posted30Days,
            snapshot.PostingRate30Days,
            snapshot.BacklogCount,
            snapshot.Backlog7DaysPlus,
            snapshot.Backlog30DaysPlus,
            snapshot.OldestOpenAgeDays,
            snapshot.LastActivityDate,
            snapshot.PreviousMonthPostingRate,
            snapshot.HistoricalPostingRate,
            snapshot.Imported30Days,
            snapshot.Platform30Days,
            snapshot.Manual30Days,
            snapshot.StornoRate30Days,
            snapshot.PreAccountedStuck,
            snapshot.DataQualityIssues,
            SplitWarnings(snapshot.WarningFlags),
            snapshot.PostingLagDfAvgDays,
            snapshot.PostingLagDfMaxDays,
            snapshot.PostingLagDfCount,
            snapshot.PostingLagOfAvgDays,
            snapshot.PostingLagOfMaxDays,
            snapshot.PostingLagOfCount,
            snapshot.PostingLagPdAvgDays,
            snapshot.PostingLagPdMaxDays,
            snapshot.PostingLagPdCount,
            snapshot.PostingLagBvAvgDays,
            snapshot.PostingLagBvMaxDays,
            snapshot.PostingLagBvCount,
            snapshot.LateEntryDfAvgDays,
            snapshot.LateEntryDfMaxDays,
            snapshot.LateEntryDfCount,
            snapshot.LateEntryOfAvgDays,
            snapshot.LateEntryOfMaxDays,
            snapshot.LateEntryOfCount,
            snapshot.LateEntryPdAvgDays,
            snapshot.LateEntryPdMaxDays,
            snapshot.LateEntryPdCount,
            snapshot.SnapshotAt.LocalDateTime.Date < DateTime.Today)).ToList();
    }

    private static IReadOnlyList<string> SplitWarnings(string warningFlags)
    {
        return string.IsNullOrWhiteSpace(warningFlags)
            ? []
            : warningFlags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
