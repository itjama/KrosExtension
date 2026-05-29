using KrosExtension.Domain.Common;

namespace KrosExtension.Domain.Configuration;

public sealed class AvailableDatabase : Entity
{
    private AvailableDatabase()
    {
    }

    public AvailableDatabase(string serverName, string databaseName, string displayName)
    {
        ServerName = serverName;
        DatabaseName = databaseName;
        DisplayName = displayName;
    }

    public string ServerName { get; private set; } = string.Empty;

    public string DatabaseName { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;
}
