using KrosExtension.Application.Configuration;
using KrosExtension.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KrosExtension.Infrastructure.Configuration;

public sealed class DataReadServerSecretMigrator(ApplicationDbContext dbContext) : IDataReadServerSecretMigrator
{
    public async Task<int> EncryptPlaintextSecretsAsync(CancellationToken ct = default)
    {
        var ids = await dbContext.Database
            .SqlQuery<Guid>($"SELECT Id AS Value FROM DataReadServers WHERE (OlympPassword <> '' AND OlympPassword NOT LIKE 'enc:%') OR (OmegaPassword <> '' AND OmegaPassword NOT LIKE 'enc:%')")
            .ToListAsync(ct);

        if (ids.Count == 0)
        {
            return 0;
        }

        var servers = await dbContext.DataReadServers
            .Where(server => ids.Contains(server.Id))
            .ToListAsync(ct);

        foreach (var server in servers)
        {
            dbContext.Entry(server).Property(s => s.OlympPassword).IsModified = true;
            dbContext.Entry(server).Property(s => s.OmegaPassword).IsModified = true;
        }

        await dbContext.SaveChangesAsync(ct);
        return servers.Count;
    }
}
