using KrosExtension.Domain.Configuration;
using Microsoft.Data.SqlClient;

namespace KrosExtension.Infrastructure.Configuration;

public static class DataReadServerConnectionStringFactory
{
    public static string? CreateMasterConnectionString(DataReadServer server, ProductType productType)
    {
        return CreateConnectionString(server, productType, "master");
    }

    public static string? CreateDatabaseConnectionString(
        DataReadServer server,
        ProductType productType,
        string databaseName)
    {
        return CreateConnectionString(server, productType, databaseName);
    }

    private static string? CreateConnectionString(
        DataReadServer server,
        ProductType productType,
        string initialCatalog)
    {
        var authenticationMode = productType == ProductType.Olymp
            ? server.OlympAuthenticationMode
            : server.OmegaAuthenticationMode;

        var userName = productType == ProductType.Olymp
            ? server.OlympUserName
            : server.OmegaUserName;

        var password = productType == ProductType.Olymp
            ? server.OlympPassword
            : server.OmegaPassword;

        if (authenticationMode == ProductAuthenticationMode.SqlLogin &&
            (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password)))
        {
            return null;
        }

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server.ServerName,
            InitialCatalog = initialCatalog,
            TrustServerCertificate = server.TrustServerCertificate,
            ApplicationName = "KrosExtension",
            IntegratedSecurity = authenticationMode == ProductAuthenticationMode.Windows
        };

        if (authenticationMode == ProductAuthenticationMode.SqlLogin)
        {
            builder.UserID = userName;
            builder.Password = password;
        }

        return builder.ConnectionString;
    }
}
