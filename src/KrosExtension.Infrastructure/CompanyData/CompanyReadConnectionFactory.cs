using System.Data.Common;
using KrosExtension.Application.CompanyData;
using KrosExtension.Domain.Configuration;
using Microsoft.Data.SqlClient;

namespace KrosExtension.Infrastructure.CompanyData;

public sealed class CompanyReadConnectionFactory : ICompanyReadConnectionFactory
{
    public DbConnection CreateConnection(CompanyDataSource dataSource)
    {
        var builder = new SqlConnectionStringBuilder(dataSource.ReadOnlyConnectionString)
        {
            InitialCatalog = dataSource.DatabaseName,
            ApplicationIntent = ApplicationIntent.ReadOnly
        };

        return new SqlConnection(builder.ConnectionString);
    }
}
