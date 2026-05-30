using KrosExtension.Application.Companies;
using KrosExtension.Domain.Configuration;
using KrosExtension.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KrosExtension.Infrastructure.Companies;

public sealed class CompanyDataRefreshService(
    ApplicationDbContext dbContext,
    ILogger<CompanyDataRefreshService> logger) : ICompanyDataRefreshService
{
    public async Task<CompanyDataRefreshResult> UpdateDataAsync(
        ProductType productType,
        string? databaseName = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.TrackedCompanies
            .Where(company => company.ProductType == productType && company.IsTracked);

        if (!string.IsNullOrWhiteSpace(databaseName))
        {
            query = query.Where(company => company.DatabaseName == databaseName);
        }

        var companies = await query
            .OrderBy(company => company.CompanyName)
            .ToListAsync(cancellationToken);

        var servers = await dbContext.DataReadServers
            .AsNoTracking()
            .Where(server => server.IsActive)
            .ToListAsync(cancellationToken);

        var skipped = new List<string>();
        var refreshed = 0;

        foreach (var company in companies)
        {
            try
            {
                var server = servers.FirstOrDefault(item => item.Id == company.DataReadServerId);

                if (server is null)
                {
                    skipped.Add(company.DatabaseName);
                    continue;
                }

                var companyInfo = await CompanyDatabaseInfoReader.ReadAsync(
                    server,
                    productType,
                    company.DatabaseName,
                    cancellationToken);

                if (companyInfo.Name is null)
                {
                    skipped.Add(company.DatabaseName);
                    continue;
                }

                company.Update(
                    CompanyDatabaseInfoReader.FormatCompanyName(companyInfo.Name, companyInfo.AccountingYear),
                    true,
                    companyInfo.AccountingYear);

                if (productType == ProductType.Omega)
                {
                    var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
                    var outdated = await dbContext.OmegaCompanyDashboardSnapshots
                        .Where(s => s.DataReadServerId == company.DataReadServerId
                                 && s.DatabaseName == company.DatabaseName
                                 && s.SnapshotAt < cutoff)
                        .ToListAsync(cancellationToken);

                    if (outdated.Count > 0)
                    {
                        dbContext.OmegaCompanyDashboardSnapshots.RemoveRange(outdated);
                    }

                    var snapshot = await OmegaDashboardSnapshotCalculator.ReadSnapshotAsync(
                        server,
                        company,
                        DateTime.Today,
                        cancellationToken);

                    if (snapshot is not null)
                    {
                        dbContext.OmegaCompanyDashboardSnapshots.Add(snapshot);
                    }
                }

                if (productType == ProductType.Olymp)
                {
                    var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
                    var outdated = await dbContext.OlympCompanyDashboardSnapshots
                        .Where(s => s.DataReadServerId == company.DataReadServerId
                                 && s.DatabaseName == company.DatabaseName
                                 && s.SnapshotAt < cutoff)
                        .ToListAsync(cancellationToken);

                    if (outdated.Count > 0)
                    {
                        dbContext.OlympCompanyDashboardSnapshots.RemoveRange(outdated);
                    }

                    var snapshot = await OlympDashboardSnapshotCalculator.ReadSnapshotAsync(
                        server,
                        company,
                        DateTime.Today,
                        cancellationToken);

                    if (snapshot is not null)
                    {
                        dbContext.OlympCompanyDashboardSnapshots.Add(snapshot);
                    }
                }

                refreshed++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to refresh company {DatabaseName} — skipping", company.DatabaseName);
                skipped.Add(company.DatabaseName);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new CompanyDataRefreshResult(
            productType,
            companies.Count,
            refreshed,
            skipped,
            DateTimeOffset.UtcNow);
    }
}
