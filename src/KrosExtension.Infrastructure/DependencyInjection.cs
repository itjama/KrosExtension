using KrosExtension.Application.CompanyData;
using KrosExtension.Application.Companies;
using KrosExtension.Application.Configuration;
using KrosExtension.Infrastructure.CompanyData;
using KrosExtension.Infrastructure.Companies;
using KrosExtension.Infrastructure.Configuration;
using KrosExtension.Application.Abstractions;
using KrosExtension.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KrosExtension.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.Configure<DatabaseCatalogOptions>(
            configuration.GetSection(DatabaseCatalogOptions.SectionName));

        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<IDatabaseCatalogReader, DatabaseCatalogReader>();
        services.AddScoped<ICompanyDiscoveryReader, CompanyDiscoveryReader>();
        services.AddScoped<ICompanyStatisticsReader, CompanyStatisticsReader>();
        services.AddScoped<ICompanyDataRefreshService, CompanyDataRefreshService>();
        services.AddScoped<IOmegaDashboardReader, OmegaDashboardReader>();
        services.AddScoped<IOmegaCompanyDetailReader, OmegaCompanyDetailReader>();
        services.AddScoped<IOmegaOrphanPaymentsReader, OmegaOrphanPaymentsReader>();
        services.AddScoped<IOlympDashboardReader, OlympDashboardReader>();
        services.AddScoped<IOlympCompanyDetailReader, OlympCompanyDetailReader>();
        services.AddScoped<ICompanyReadConnectionFactory, CompanyReadConnectionFactory>();

        return services;
    }
}
