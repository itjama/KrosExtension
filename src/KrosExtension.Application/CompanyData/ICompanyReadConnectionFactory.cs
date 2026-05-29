using System.Data.Common;
using KrosExtension.Domain.Configuration;

namespace KrosExtension.Application.CompanyData;

public interface ICompanyReadConnectionFactory
{
    DbConnection CreateConnection(CompanyDataSource dataSource);
}
