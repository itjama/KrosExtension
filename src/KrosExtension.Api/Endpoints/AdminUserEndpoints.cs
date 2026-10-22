using System.Security.Claims;
using KrosExtension.Api.Auth;
using KrosExtension.Application.Users;
using KrosExtension.Domain.Users;
using Microsoft.AspNetCore.Mvc;

namespace KrosExtension.Api.Endpoints;

public static class AdminUserEndpoints
{
    private const string InvalidRoleMessage = "Rola je povinná.";
    private const string CannotResetOwnPasswordMessage = "Vlastné heslo si zmeň cez Zmeniť heslo.";

    public static IEndpointRouteBuilder MapAdminUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/admin/users")
            .WithTags("Admin users")
            .RequireAuthorization(AuthPolicies.Admin);

        group.MapGet("/", async (IUserAdministrationService service, TimeProvider timeProvider, CancellationToken ct) =>
            {
                var users = await service.GetUsersAsync(ct);
                var now = timeProvider.GetUtcNow();
                return Results.Ok(users.Select(user => AdminUserResponse.From(user, now)));
            })
            .WithName("GetAdminUsers");

        group.MapPost("/", async (
                [FromBody] CreateUserRequest? request,
                IUserAdministrationService service,
                TimeProvider timeProvider,
                CancellationToken ct) =>
            {
                if (request?.Role is { } requestedRole && !Enum.IsDefined(requestedRole))
                {
                    return Results.BadRequest(InvalidRoleMessage);
                }

                var result = await service.CreateUserAsync(
                    request?.UserName ?? string.Empty,
                    request?.Role ?? UserRole.User,
                    ct);

                if (result.Error != UserAdminError.None || result.User is null)
                {
                    return ToError(result.Error);
                }

                return Results.Created(
                    $"/api/admin/users/{result.User.Id}",
                    new CreateUserResponse(AdminUserResponse.From(result.User, timeProvider.GetUtcNow()), result.TemporaryPassword));
            })
            .WithName("CreateAdminUser");

        group.MapPost("/{id:guid}/reset-password", async (
                Guid id,
                ClaimsPrincipal principal,
                IUserAdministrationService service,
                CancellationToken ct) =>
            {
                if (!TryGetUserId(principal, out var actingUserId))
                {
                    return Results.Unauthorized();
                }

                var result = await service.ResetPasswordAsync(actingUserId, id, ct);
                return result.Error switch
                {
                    UserAdminError.None => Results.Ok(new TemporaryPasswordResponse(result.TemporaryPassword)),
                    UserAdminError.CannotModifySelf => Results.BadRequest(CannotResetOwnPasswordMessage),
                    _ => ToError(result.Error),
                };
            })
            .WithName("ResetAdminUserPassword");

        group.MapPost("/{id:guid}/block", async (
                Guid id,
                ClaimsPrincipal principal,
                IUserAdministrationService service,
                CancellationToken ct) =>
            {
                if (!TryGetUserId(principal, out var actingUserId))
                {
                    return Results.Unauthorized();
                }

                var result = await service.BlockAsync(actingUserId, id, ct);
                return result.Error == UserAdminError.None ? Results.NoContent() : ToError(result.Error);
            })
            .WithName("BlockAdminUser");

        group.MapPost("/{id:guid}/unblock", async (
                Guid id,
                IUserAdministrationService service,
                CancellationToken ct) =>
            {
                var result = await service.UnblockAsync(id, ct);
                return result.Error == UserAdminError.None ? Results.NoContent() : ToError(result.Error);
            })
            .WithName("UnblockAdminUser");

        group.MapPut("/{id:guid}/role", async (
                Guid id,
                [FromBody] ChangeRoleRequest? request,
                ClaimsPrincipal principal,
                IUserAdministrationService service,
                CancellationToken ct) =>
            {
                if (!TryGetUserId(principal, out var actingUserId))
                {
                    return Results.Unauthorized();
                }

                if (request?.Role is not { } role || !Enum.IsDefined(role))
                {
                    return Results.BadRequest(InvalidRoleMessage);
                }

                var result = await service.ChangeRoleAsync(actingUserId, id, role, ct);
                return result.Error == UserAdminError.None ? Results.NoContent() : ToError(result.Error);
            })
            .WithName("ChangeAdminUserRole");

        return endpoints;
    }

    private static IResult ToError(UserAdminError error) => error switch
    {
        UserAdminError.NotFound => Results.NotFound(),
        UserAdminError.EmptyUserName => Results.BadRequest("Meno je povinné."),
        UserAdminError.DuplicateUserName => Results.Conflict("Používateľ s týmto menom už existuje."),
        UserAdminError.CannotModifySelf => Results.BadRequest("Nemôžeš zablokovať ani zmeniť rolu sám sebe."),
        UserAdminError.LastActiveAdmin => Results.BadRequest("Musí zostať aspoň jeden aktívny admin."),
        _ => Results.BadRequest(),
    };

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    public sealed record CreateUserRequest(string? UserName, UserRole? Role);

    public sealed record ChangeRoleRequest(UserRole? Role);

    public sealed record TemporaryPasswordResponse(string? TemporaryPassword);

    public sealed record CreateUserResponse(AdminUserResponse User, string? TemporaryPassword);

    public sealed record AdminUserResponse(
        Guid Id,
        string UserName,
        string Role,
        bool IsBlocked,
        bool MustChangePassword,
        bool IsLockedOut,
        DateTimeOffset CreatedAt,
        DateTimeOffset? LastLoginAt)
    {
        public static AdminUserResponse From(AppUser user, DateTimeOffset now) => new(
            user.Id,
            user.UserName,
            user.Role.ToString(),
            user.IsBlocked,
            user.MustChangePassword,
            user.IsLockedOut(now),
            user.CreatedAt,
            user.LastLoginAt);
    }
}
