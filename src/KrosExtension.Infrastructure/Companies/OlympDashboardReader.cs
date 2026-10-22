using KrosExtension.Application.Companies;
using KrosExtension.Domain.Configuration;
using KrosExtension.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KrosExtension.Infrastructure.Companies;

public sealed class OlympDashboardReader(ApplicationDbContext dbContext) : IOlympDashboardReader
{
    public async Task<IReadOnlyList<OlympDashboardCompany>> GetCompaniesAsync(
        CancellationToken cancellationToken = default)
    {
        var trackedKeys = await dbContext.TrackedCompanies
            .AsNoTracking()
            .Where(tc => tc.ProductType == ProductType.Olymp && tc.IsTracked)
            .Select(tc => new { tc.DataReadServerId, tc.DatabaseName })
            .ToListAsync(cancellationToken);

        var trackedSet = trackedKeys
            .Select(k => (k.DataReadServerId, k.DatabaseName))
            .ToHashSet();

        var snapshots = await dbContext.OlympCompanyDashboardSnapshots
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var latest = snapshots
            .Where(s => trackedSet.Contains((s.DataReadServerId, s.DatabaseName)))
            .GroupBy(s => new { s.DataReadServerId, s.DatabaseName })
            .Select(g => g.OrderByDescending(s => s.SnapshotAt).First())
            .OrderBy(s => s.HealthScore)
            .ThenBy(s => s.LastActivityDate)
            .ToList();

        return latest.Select(s => new OlympDashboardCompany(
            s.DataReadServerId,
            s.ServerDisplayName,
            s.DatabaseName,
            s.CompanyName,
            s.AccountingYear,
            s.SnapshotAt,
            s.HealthScore,
            s.ActivityScore,
            s.PayrollCurrentnessScore,
            s.PayrollConsistencyScore,
            s.PeriodRegularityScore,
            s.LastActivityDate,
            s.LastProcessedPeriod,
            s.CurrentMonthProcessed,
            s.ProcessedPeriodsInYear,
            s.Activity7Days,
            s.Activity30Days,
            s.TrendPercent,
            SplitWarnings(s.WarningFlags),
            s.SnapshotAt.LocalDateTime.Date < DateTime.Today)).ToList();
    }

    private static IReadOnlyList<string> SplitWarnings(string flags) =>
        string.IsNullOrWhiteSpace(flags)
            ? []
            : flags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
