using KrosExtension.Application.Companies;
using KrosExtension.Domain.Configuration;
using KrosExtension.Infrastructure.Configuration;
using KrosExtension.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KrosExtension.Infrastructure.Companies;

public sealed class OmegaOrphanPaymentsReader(ApplicationDbContext dbContext) : IOmegaOrphanPaymentsReader
{
    public async Task<OmegaOrphanPaymentsResult?> GetOrphanPaymentsAsync(
        Guid dataReadServerId,
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        var server = await dbContext.DataReadServers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == dataReadServerId && s.IsActive, cancellationToken);

        if (server is null)
        {
            return null;
        }

        var connectionString = DataReadServerConnectionStringFactory.CreateDatabaseConnectionString(
            server, ProductType.Omega, databaseName);

        if (connectionString is null)
        {
            return null;
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var totalCount = await ReadTotalCountAsync(connection, cancellationToken);
        var items = await ReadItemsAsync(connection, cancellationToken);

        return new OmegaOrphanPaymentsResult(totalCount, items);
    }

    private static async Task<long> ReadTotalCountAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = CountSql;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? 0 : Convert.ToInt64(result);
    }

    private static async Task<IReadOnlyList<OmegaOrphanPayment>> ReadItemsAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = DetailSql;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var list = new List<OmegaOrphanPayment>();

        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new OmegaOrphanPayment(
                GetInt64(reader, "PaymentId"),
                GetInt64(reader, "OrphanedDocumentId"),
                GetString(reader, "DocumentType"),
                GetString(reader, "InternalNumber"),
                GetNullableDateString(reader, "PaymentDate"),
                GetDecimal(reader, "Amount"),
                GetString(reader, "Currency"),
                GetString(reader, "PaymentKind"),
                GetNullableString(reader, "Note")));
        }

        return list;
    }

    private static long GetInt64(SqlDataReader reader, string name)
    {
        var value = reader[name];
        return value is null or DBNull ? 0 : Convert.ToInt64(value);
    }

    private static decimal GetDecimal(SqlDataReader reader, string name)
    {
        var value = reader[name];
        return value is null or DBNull ? 0m : Convert.ToDecimal(value);
    }

    private static string GetString(SqlDataReader reader, string name)
    {
        var value = reader[name];
        return value is null or DBNull ? string.Empty : value.ToString()!;
    }

    private static string? GetNullableString(SqlDataReader reader, string name)
    {
        var value = reader[name];
        if (value is null or DBNull) return null;
        var s = value.ToString();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    private static string? GetNullableDateString(SqlDataReader reader, string name)
    {
        var value = reader[name];
        return value is null or DBNull ? null : Convert.ToDateTime(value).ToString("yyyy-MM-dd");
    }

    private const string CountSql = """
        SELECT COUNT_BIG(*)
        FROM dbo.T047_EUD_UhradyOdpocty AS p
        LEFT JOIN dbo.T040_EUD AS h ON h.C000_ID = p.C010_IDEUD
        WHERE h.C000_ID IS NULL
        """;

    private const string DetailSql = """
        SELECT TOP 50
            p.C000_ID                                                            AS PaymentId,
            p.C010_IDEUD                                                         AS OrphanedDocumentId,
            ISNULL(p.C023_CisloInterneKodOkruh, '')                             AS DocumentType,
            ISNULL(p.C030_CisloInterne, '')                                     AS InternalNumber,
            TRY_CONVERT(date, CONCAT(p.C062_Rok,'-',p.C061_Mes,'-',p.C060_Den)) AS PaymentDate,
            ISNULL(p.C211_SumaSpolu, 0)                                         AS Amount,
            ISNULL(p.C200_Mena, '')                                             AS Currency,
            CASE WHEN ISNULL(p.C103_VzajomnyZapocet, 0) <> 0
                 THEN 'zápočet' ELSE 'platba' END                               AS PaymentKind,
            CAST(p.C097_Poznamka AS nvarchar(500))                              AS Note
        FROM dbo.T047_EUD_UhradyOdpocty AS p
        LEFT JOIN dbo.T040_EUD AS h ON h.C000_ID = p.C010_IDEUD
        WHERE h.C000_ID IS NULL
        ORDER BY p.C010_IDEUD, p.C000_ID
        """;
}
