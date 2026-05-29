namespace KrosExtension.Application.Configuration;

public sealed record DatabaseCatalogItem(
    string ServerName,
    string DatabaseName,
    bool IsOnline,
    bool IsReadOnly);
