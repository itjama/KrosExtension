using KrosExtension.Application.Configuration;
using KrosExtension.Domain.Configuration;
using Microsoft.Data.SqlClient;

namespace KrosExtension.Infrastructure.Configuration;

public sealed class DatabaseCatalogReader(ISettingsRepository settingsRepository)
    : IDatabaseCatalogReader
{
    public async Task<IReadOnlyList<DatabaseCatalogItem>> GetDatabasesAsync(
        ProductType productType,
        CancellationToken cancellationToken = default)
    {
        var databases = new List<DatabaseCatalogItem>();
        var servers = await settingsRepository.GetDataReadServersAsync(cancellationToken);

        foreach (var server in servers.Where(server => server.IsActive))
        {
            var connectionString = DataReadServerConnectionStringFactory.CreateMasterConnectionString(
                server,
                productType);

            if (connectionString is null)
            {
                continue;
            }

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT name, state_desc, is_read_only
                FROM sys.databases
                WHERE database_id > 4
                ORDER BY name
                """;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                databases.Add(new DatabaseCatalogItem(
                    server.DisplayName,
                    reader.GetString(0),
                    string.Equals(reader.GetString(1), "ONLINE", StringComparison.OrdinalIgnoreCase),
                    reader.GetBoolean(2)));
            }
        }

        return databases;
    }
}
