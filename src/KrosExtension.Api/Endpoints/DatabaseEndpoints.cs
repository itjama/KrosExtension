using KrosExtension.Api.Auth;
using KrosExtension.Application.Configuration;
using KrosExtension.Domain.Configuration;

namespace KrosExtension.Api.Endpoints;

public static class DatabaseEndpoints
{
    public static IEndpointRouteBuilder MapDatabaseEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/databases")
            .WithTags("Databases")
            .RequireAuthorization(AuthPolicies.Admin);

        group.MapGet("/catalog/{productType}", async (
            ProductType productType,
            IDatabaseCatalogReader catalogReader,
            CancellationToken cancellationToken) =>
        {
            var databases = await catalogReader.GetDatabasesAsync(productType, cancellationToken);
            return Results.Ok(databases);
        }).WithName("GetDatabaseCatalog");

        group.MapGet("/configured", async (
            ISettingsRepository settingsRepository,
            CancellationToken cancellationToken) =>
        {
            var databases = await settingsRepository.GetAvailableDatabasesAsync(cancellationToken);
            return Results.Ok(databases.Select(database => new
            {
                database.Id,
                database.ServerName,
                database.DatabaseName,
                database.DisplayName,
                database.IsActive
            }));
        }).WithName("GetConfiguredDatabases");

        return endpoints;
    }
}
