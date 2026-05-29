using KrosExtension.Application.Companies;
using KrosExtension.Application.Configuration;
using KrosExtension.Domain.Configuration;

namespace KrosExtension.Api.Endpoints;

public static class ProductCompanyEndpoints
{
    public static IEndpointRouteBuilder MapProductCompanyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        MapProduct(endpoints, "olymp", ProductType.Olymp);
        MapProduct(endpoints, "omega", ProductType.Omega);

        return endpoints;
    }

    private static void MapProduct(IEndpointRouteBuilder endpoints, string route, ProductType productType)
    {
        var productGroup = endpoints
            .MapGroup($"/api/{route}")
            .WithTags($"{productType}");

        productGroup.MapGet("/refresh-settings", async (
            ISettingsRepository settingsRepository,
            CancellationToken cancellationToken) =>
        {
            var settings = await settingsRepository.GetProductRefreshSettingsAsync(productType, cancellationToken);
            return Results.Ok(new ProductRefreshSettingsResponse(
                settings.ProductType,
                settings.RefreshTime.ToString("HH:mm")));
        }).WithName($"Get{productType}RefreshSettings");

        productGroup.MapPut("/refresh-settings", async (
            SaveProductRefreshSettingsRequest request,
            ISettingsRepository settingsRepository,
            CancellationToken cancellationToken) =>
        {
            if (!TimeOnly.TryParse(request.RefreshTime, out var refreshTime))
            {
                return Results.BadRequest("RefreshTime must be a valid time, for example 02:00.");
            }

            await settingsRepository.SaveProductRefreshSettingsAsync(productType, refreshTime, cancellationToken);
            return Results.NoContent();
        }).WithName($"Save{productType}RefreshSettings");

        if (productType == ProductType.Omega)
        {
            productGroup.MapGet("/dashboard/companies", async (
                IOmegaDashboardReader dashboardReader,
                CancellationToken cancellationToken) =>
            {
                var companies = await dashboardReader.GetCompaniesAsync(cancellationToken);
                return Results.Ok(companies);
            }).WithName("GetOmegaDashboardCompanies");
        }

        var group = endpoints
            .MapGroup($"/api/{route}/companies")
            .WithTags($"{productType} companies");

        group.MapGet("/discover", async (
            ICompanyDiscoveryReader discoveryReader,
            CancellationToken cancellationToken) =>
        {
            var companies = await discoveryReader.DiscoverCompaniesAsync(productType, cancellationToken);
            return Results.Ok(companies);
        }).WithName($"Discover{productType}Companies");

        group.MapGet("/tracked", async (
            ISettingsRepository settingsRepository,
            CancellationToken cancellationToken) =>
        {
            var companies = await settingsRepository.GetTrackedCompaniesAsync(productType, cancellationToken);
            return Results.Ok(companies.Select(company => new TrackedCompanyResponse(
                company.Id,
                company.ProductType,
                company.DataReadServerId,
                company.DatabaseName,
                company.CompanyName,
                company.AccountingYear,
                company.IsTracked)));
        }).WithName($"GetTracked{productType}Companies");

        group.MapPut("/tracked", async (
            SaveTrackedCompaniesRequest request,
            ISettingsRepository settingsRepository,
            CancellationToken cancellationToken) =>
        {
            var selections = request.Companies
                .Select(company => new TrackedCompanySelection(
                    productType,
                    company.DataReadServerId,
                    company.DatabaseName,
                    company.CompanyName,
                    company.IsTracked))
                .ToList();

            await settingsRepository.SaveTrackedCompaniesAsync(productType, selections, cancellationToken);
            return Results.NoContent();
        }).WithName($"SaveTracked{productType}Companies");

        group.MapGet("/statistics", async (
            ICompanyStatisticsReader statisticsReader,
            CancellationToken cancellationToken) =>
        {
            var statistics = await statisticsReader.GetStatisticsAsync(productType, cancellationToken);
            return Results.Ok(statistics);
        }).WithName($"Get{productType}CompanyStatistics");

        group.MapPost("/update-data", async (
            UpdateCompanyDataRequest request,
            ICompanyDataRefreshService refreshService,
            CancellationToken cancellationToken) =>
        {
            var result = await refreshService.UpdateDataAsync(
                productType,
                request.DatabaseName,
                cancellationToken);

            return Results.Ok(result);
        }).WithName($"Update{productType}CompanyData");
    }
}

public sealed record SaveTrackedCompaniesRequest(IReadOnlyList<TrackedCompanySelectionRequest> Companies);

public sealed record TrackedCompanySelectionRequest(
    Guid DataReadServerId,
    string DatabaseName,
    string CompanyName,
    bool IsTracked);

public sealed record TrackedCompanyResponse(
    Guid Id,
    ProductType ProductType,
    Guid DataReadServerId,
    string DatabaseName,
    string CompanyName,
    int? AccountingYear,
    bool IsTracked);

public sealed record ProductRefreshSettingsResponse(
    ProductType ProductType,
    string RefreshTime);

public sealed record SaveProductRefreshSettingsRequest(string RefreshTime);

public sealed record UpdateCompanyDataRequest(string? DatabaseName);
