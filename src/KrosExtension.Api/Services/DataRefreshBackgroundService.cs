using KrosExtension.Application.Companies;
using KrosExtension.Application.Configuration;
using KrosExtension.Domain.Configuration;

namespace KrosExtension.Api.Services;

public sealed class DataRefreshBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<DataRefreshBackgroundService> logger) : BackgroundService
{
    private DateOnly _omegaLastRunDate = DateOnly.MinValue;
    private DateOnly _olympLastRunDate = DateOnly.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            var now = TimeOnly.FromDateTime(DateTime.Now);
            var today = DateOnly.FromDateTime(DateTime.Today);

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var settings = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();

                var omegaSettings = await settings.GetProductRefreshSettingsAsync(ProductType.Omega, stoppingToken);
                var olympSettings = await settings.GetProductRefreshSettingsAsync(ProductType.Olymp, stoppingToken);

                if (today > _omegaLastRunDate && now >= omegaSettings.RefreshTime)
                {
                    _omegaLastRunDate = today;
                    logger.LogInformation("Auto-refresh: starting Omega UpdateData at {Time}", now);
                    var refreshService = scope.ServiceProvider.GetRequiredService<ICompanyDataRefreshService>();
                    var result = await refreshService.UpdateDataAsync(ProductType.Omega, null, stoppingToken);
                    logger.LogInformation(
                        "Auto-refresh: Omega done — {Refreshed}/{Total} companies refreshed",
                        result.RefreshedCompanies, result.RequestedCompanies);
                }

                if (today > _olympLastRunDate && now >= olympSettings.RefreshTime)
                {
                    _olympLastRunDate = today;
                    logger.LogInformation("Auto-refresh: starting Olymp UpdateData at {Time}", now);
                    var refreshService = scope.ServiceProvider.GetRequiredService<ICompanyDataRefreshService>();
                    var result = await refreshService.UpdateDataAsync(ProductType.Olymp, null, stoppingToken);
                    logger.LogInformation(
                        "Auto-refresh: Olymp done — {Refreshed}/{Total} companies refreshed",
                        result.RefreshedCompanies, result.RequestedCompanies);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Auto-refresh failed");
            }
        }
    }
}
