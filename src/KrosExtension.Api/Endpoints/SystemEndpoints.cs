namespace KrosExtension.Api.Endpoints;

public static class SystemEndpoints
{
    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/system")
            .WithTags("System");

        group.MapGet("/info", () => Results.Ok(new
            {
                Application = "Kros Extension Backend",
                Status = "Running",
                ServerTime = DateTimeOffset.UtcNow
            }))
            .WithName("GetSystemInfo");

        return endpoints;
    }
}
