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

    public async Task<IReadOnlyList<UndecryptableSecret>> FindUndecryptableSecretsAsync(CancellationToken ct = default)
    {
        var rawRows = await dbContext.Database
            .SqlQuery<RawSecretRow>($"SELECT Id, OlympPassword, OmegaPassword FROM DataReadServers WHERE OlympPassword LIKE 'enc:%' OR OmegaPassword LIKE 'enc:%'")
            .ToListAsync(ct);

        if (rawRows.Count == 0)
        {
            return [];
        }

        var ids = rawRows.Select(row => row.Id).ToList();

        // Loading through EF runs SecretValueConverter, which yields an empty string for ciphertext
        // the key ring cannot decrypt. A non-empty value is never stored as empty ciphertext, so
        // "raw starts with enc: but decrypts to empty" means the secret is unrecoverable.
        var decrypted = await dbContext.DataReadServers
            .AsNoTracking()
            .Where(server => ids.Contains(server.Id))
            .Select(server => new { server.Id, server.OlympPassword, server.OmegaPassword })
            .ToDictionaryAsync(server => server.Id, ct);

        var result = new List<UndecryptableSecret>();
        foreach (var row in rawRows)
        {
            if (!decrypted.TryGetValue(row.Id, out var server))
            {
                continue;
            }

            if (IsEncrypted(row.OlympPassword) && string.IsNullOrEmpty(server.OlympPassword))
            {
                result.Add(new UndecryptableSecret(row.Id, nameof(RawSecretRow.OlympPassword)));
            }

            if (IsEncrypted(row.OmegaPassword) && string.IsNullOrEmpty(server.OmegaPassword))
            {
                result.Add(new UndecryptableSecret(row.Id, nameof(RawSecretRow.OmegaPassword)));
            }
        }

        return result;
    }

    private static bool IsEncrypted(string? raw) =>
        raw is not null && raw.StartsWith(SecretValueConverter.Prefix, StringComparison.Ordinal);

    private sealed record RawSecretRow(Guid Id, string? OlympPassword, string? OmegaPassword);
}
