namespace KrosExtension.Infrastructure.Configuration;

public sealed class DatabaseCatalogOptions
{
    public const string SectionName = "DatabaseCatalog";

    public List<DatabaseServerOptions> Servers { get; init; } = [];
}
