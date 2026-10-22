using KrosExtension.Api.Auth;
using KrosExtension.Application.Configuration;
using KrosExtension.Domain.Configuration;

namespace KrosExtension.Api.Endpoints;

public static class DataReadServerEndpoints
{
    public static IEndpointRouteBuilder MapDataReadServerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/data-read-servers")
            .WithTags("Data read servers")
            .RequireAuthorization(AuthPolicies.Admin);

        group.MapGet("/", async (
            ISettingsRepository settingsRepository,
            CancellationToken cancellationToken) =>
        {
            var servers = await settingsRepository.GetDataReadServersAsync(cancellationToken);
            return Results.Ok(servers.Select(ToResponse));
        }).WithName("GetDataReadServers");

        group.MapPost("/", async (
            AddDataReadServerRequest request,
            ISettingsRepository settingsRepository,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.ServerName))
            {
                return Results.BadRequest("ServerName is required.");
            }

            if (!request.HasOlympCredentials && !request.HasOmegaCredentials)
            {
                return Results.BadRequest("At least Olymp or Omega credentials are required.");
            }

            var serverExists = await settingsRepository.DataReadServerExistsAsync(
                request.ServerName,
                cancellationToken);

            if (serverExists)
            {
                return Results.Conflict($"Data read server '{request.ServerName}' already exists.");
            }

            var server = new DataReadServer(
                request.DisplayName ?? request.ServerName,
                request.ServerName,
                request.Olymp?.AuthenticationMode ?? ProductAuthenticationMode.Windows,
                request.Olymp?.UserName,
                request.Olymp?.Password,
                request.Omega?.AuthenticationMode ?? ProductAuthenticationMode.Windows,
                request.Omega?.UserName,
                request.Omega?.Password,
                request.TrustServerCertificate);

            var savedServer = await settingsRepository.AddDataReadServerAsync(server, cancellationToken);

            return Results.Created($"/api/data-read-servers/{savedServer.Id}", ToResponse(savedServer));
        }).WithName("AddDataReadServer");

        group.MapDelete("/{id:guid}", async (
            Guid id,
            ISettingsRepository settingsRepository,
            CancellationToken cancellationToken) =>
        {
            var deleted = await settingsRepository.DeleteDataReadServerAsync(id, cancellationToken);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).WithName("DeleteDataReadServer");

        return endpoints;
    }

    private static DataReadServerResponse ToResponse(DataReadServer server)
    {
        return new DataReadServerResponse(
            server.Id,
            server.DisplayName,
            server.ServerName,
            server.OlympAuthenticationMode,
            server.HasOlympCredentials,
            server.OmegaAuthenticationMode,
            server.HasOmegaCredentials,
            server.TrustServerCertificate,
            server.IsActive);
    }
}

public sealed record AddDataReadServerRequest(
    string ServerName,
    string? DisplayName,
    ProductCredentialsRequest? Olymp,
    ProductCredentialsRequest? Omega,
    bool TrustServerCertificate = true)
{
    public bool HasOlympCredentials =>
        Olymp?.AuthenticationMode == ProductAuthenticationMode.Windows ||
        (!string.IsNullOrWhiteSpace(Olymp?.UserName) && !string.IsNullOrWhiteSpace(Olymp?.Password));

    public bool HasOmegaCredentials =>
        Omega?.AuthenticationMode == ProductAuthenticationMode.Windows ||
        (!string.IsNullOrWhiteSpace(Omega?.UserName) && !string.IsNullOrWhiteSpace(Omega?.Password));
}

public sealed record ProductCredentialsRequest(
    ProductAuthenticationMode AuthenticationMode,
    string? UserName,
    string? Password);

public sealed record DataReadServerResponse(
    Guid Id,
    string DisplayName,
    string ServerName,
    ProductAuthenticationMode OlympAuthenticationMode,
    bool HasOlympCredentials,
    ProductAuthenticationMode OmegaAuthenticationMode,
    bool HasOmegaCredentials,
    bool TrustServerCertificate,
    bool IsActive);
