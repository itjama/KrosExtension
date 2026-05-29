using KrosExtension.Application.Companies;
using KrosExtension.Application.Configuration;
using KrosExtension.Domain.Configuration;
using KrosExtension.Infrastructure.Companies;
using KrosExtension.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KrosExtension.Infrastructure.Configuration;

public sealed class SettingsRepository(ApplicationDbContext dbContext) : ISettingsRepository
{
    public async Task<DataReadServer> AddDataReadServerAsync(
        DataReadServer server,
        CancellationToken cancellationToken = default)
    {
        dbContext.DataReadServers.Add(server);
        await dbContext.SaveChangesAsync(cancellationToken);

        return server;
    }

    public async Task<bool> DeleteDataReadServerAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var server = await dbContext.DataReadServers
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (server is null)
        {
            return false;
        }

        dbContext.DataReadServers.Remove(server);
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DataReadServerExistsAsync(string serverName, CancellationToken cancellationToken = default)
    {
        return await dbContext.DataReadServers
            .AsNoTracking()
            .AnyAsync(
                server => server.ServerName.ToUpper() == serverName.ToUpper(),
                cancellationToken);
    }

    public async Task<IReadOnlyList<AvailableDatabase>> GetAvailableDatabasesAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.AvailableDatabases
            .AsNoTracking()
            .OrderBy(database => database.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CompanyDataSource>> GetCompanyDataSourcesAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.CompanyDataSources
            .AsNoTracking()
            .OrderBy(source => source.CompanyName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DataReadServer>> GetDataReadServersAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.DataReadServers
            .AsNoTracking()
            .OrderBy(server => server.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TrackedCompany>> GetTrackedCompaniesAsync(
        ProductType productType,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.TrackedCompanies
            .AsNoTracking()
            .Where(company => company.ProductType == productType && company.IsTracked)
            .OrderBy(company => company.CompanyName)
            .ToListAsync(cancellationToken);
    }

    public async Task<ProductRefreshSettingsDto> GetProductRefreshSettingsAsync(
        ProductType productType,
        CancellationToken cancellationToken = default)
    {
        var settings = await dbContext.ProductRefreshSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.ProductType == productType, cancellationToken);

        return new ProductRefreshSettingsDto(
            productType,
            settings?.RefreshTime ?? new TimeOnly(2, 0));
    }

    public async Task SaveProductRefreshSettingsAsync(
        ProductType productType,
        TimeOnly refreshTime,
        CancellationToken cancellationToken = default)
    {
        var settings = await dbContext.ProductRefreshSettings
            .FirstOrDefaultAsync(item => item.ProductType == productType, cancellationToken);

        if (settings is null)
        {
            dbContext.ProductRefreshSettings.Add(new ProductRefreshSettings(productType, refreshTime));
        }
        else
        {
            settings.Update(refreshTime);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveTrackedCompaniesAsync(
        ProductType productType,
        IReadOnlyList<TrackedCompanySelection> selections,
        CancellationToken cancellationToken = default)
    {
        var trackedCompanies = await dbContext.TrackedCompanies
            .Where(company => company.ProductType == productType)
            .ToListAsync(cancellationToken);

        foreach (var selection in selections)
        {
            var existing = trackedCompanies.FirstOrDefault(company =>
                company.DataReadServerId == selection.DataReadServerId &&
                string.Equals(company.DatabaseName, selection.DatabaseName, StringComparison.OrdinalIgnoreCase));

            if (existing is null)
            {
                dbContext.TrackedCompanies.Add(new TrackedCompany(
                    productType,
                    selection.DataReadServerId,
                    selection.DatabaseName,
                    selection.CompanyName,
                    CompanyDatabaseInfoReader.TryParseAccountingYear(selection.CompanyName)));

                continue;
            }

            existing.Update(
                selection.CompanyName,
                selection.IsTracked,
                CompanyDatabaseInfoReader.TryParseAccountingYear(selection.CompanyName));
        }

        var selectedKeys = selections
            .Where(selection => selection.IsTracked)
            .Select(selection => (selection.DataReadServerId, selection.DatabaseName.ToUpperInvariant()))
            .ToHashSet();

        foreach (var trackedCompany in trackedCompanies)
        {
            var key = (trackedCompany.DataReadServerId, trackedCompany.DatabaseName.ToUpperInvariant());

            if (!selectedKeys.Contains(key))
            {
                trackedCompany.Update(trackedCompany.CompanyName, false, trackedCompany.AccountingYear);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
