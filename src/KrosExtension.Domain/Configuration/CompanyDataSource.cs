using KrosExtension.Domain.Common;

namespace KrosExtension.Domain.Configuration;

public sealed class CompanyDataSource : Entity
{
    private CompanyDataSource()
    {
    }

    public CompanyDataSource(
        string companyCode,
        string companyName,
        string serverName,
        string databaseName,
        string readOnlyConnectionString)
    {
        CompanyCode = companyCode;
        CompanyName = companyName;
        ServerName = serverName;
        DatabaseName = databaseName;
        ReadOnlyConnectionString = readOnlyConnectionString;
    }

    public string CompanyCode { get; private set; } = string.Empty;

    public string CompanyName { get; private set; } = string.Empty;

    public string ServerName { get; private set; } = string.Empty;

    public string DatabaseName { get; private set; } = string.Empty;

    public string ReadOnlyConnectionString { get; private set; } = string.Empty;

    public bool IsActive { get; private set; } = true;
}
