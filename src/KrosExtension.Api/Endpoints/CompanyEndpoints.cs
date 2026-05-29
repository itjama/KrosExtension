using KrosExtension.Application.Configuration;

namespace KrosExtension.Api.Endpoints;

public static class CompanyEndpoints
{
    public static IEndpointRouteBuilder MapCompanyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/companies")
            .WithTags("Companies");

        group.MapGet("/data-sources", async (
            ISettingsRepository settingsRepository,
            CancellationToken cancellationToken) =>
        {
            var sources = await settingsRepository.GetCompanyDataSourcesAsync(cancellationToken);
            return Results.Ok(sources.Select(source => new
            {
                source.Id,
                source.CompanyCode,
                source.CompanyName,
                source.ServerName,
                source.DatabaseName,
                source.IsActive
            }));
        }).WithName("GetCompanyDataSources");

        return endpoints;
    }
}
