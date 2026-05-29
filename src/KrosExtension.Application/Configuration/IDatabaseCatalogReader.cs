using KrosExtension.Domain.Configuration;

namespace KrosExtension.Application.Configuration;

public interface IDatabaseCatalogReader
{
    Task<IReadOnlyList<DatabaseCatalogItem>> GetDatabasesAsync(
        ProductType productType,
        CancellationToken cancellationToken = default);
}
