namespace KrosExtension.Infrastructure.Configuration;

public sealed class DatabaseServerOptions
{
    public string Name { get; init; } = string.Empty;

    public string ConnectionString { get; init; } = string.Empty;
}
