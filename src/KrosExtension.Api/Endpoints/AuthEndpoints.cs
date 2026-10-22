using System.Security.Claims;
using KrosExtension.Api.Auth;
using KrosExtension.Application.Users;
using KrosExtension.Domain.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace KrosExtension.Api.Endpoints;

public static class AuthEndpoints
{
    public const string InvalidCredentialsMessage = "Nesprávne meno alebo heslo";
    public const string LockedOutMessage = "Účet je dočasne zamknutý. Skús to neskôr.";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/login", async (
                [FromBody] LoginRequest? request,
                IUserAccountService accounts,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await accounts.LoginAsync(
                    request?.UserName ?? string.Empty,
                    request?.Password ?? string.Empty,
                    cancellationToken);

                switch (result.Status)
                {
                    case LoginStatus.Success when result.User is not null:
                        await SignInAsync(httpContext, result.User);
                        return Results.Ok(UserInfoResponse.From(result.User));
                    case LoginStatus.LockedOut:
                        return Results.Text(LockedOutMessage, statusCode: StatusCodes.Status423Locked);
                    default:
                        return Results.Text(InvalidCredentialsMessage, statusCode: StatusCodes.Status401Unauthorized);
                }
            })
            .AllowAnonymous()
            .WithName("Login");

        group.MapPost("/logout", async (HttpContext httpContext) =>
            {
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return Results.NoContent();
            })
            .RequireAuthorization(AuthPolicies.PasswordChange)
            .WithName("Logout");

        group.MapGet("/me", async (
                ClaimsPrincipal principal,
                IUserAccountService accounts,
                CancellationToken cancellationToken) =>
            {
                var user = TryGetUserId(principal, out var userId)
                    ? await accounts.FindByIdAsync(userId, cancellationToken)
                    : null;
                return user is null ? Results.Unauthorized() : Results.Ok(UserInfoResponse.From(user));
            })
            .RequireAuthorization(AuthPolicies.PasswordChange)
            .WithName("GetCurrentUser");

        group.MapPost("/change-password", async (
                [FromBody] ChangePasswordRequest? request,
                ClaimsPrincipal principal,
                IUserAccountService accounts,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                if (!TryGetUserId(principal, out var userId))
                {
                    return Results.Unauthorized();
                }

                var (status, user) = await accounts.ChangePasswordAsync(
                    userId,
                    request?.CurrentPassword ?? string.Empty,
                    request?.NewPassword ?? string.Empty,
                    cancellationToken);

                switch (status)
                {
                    case PasswordChangeStatus.Success when user is not null:
                        // The security stamp changed, so the current cookie is now stale: issue a fresh one.
                        await SignInAsync(httpContext, user);
                        return Results.Ok(UserInfoResponse.From(user));
                    case PasswordChangeStatus.TooShort:
                        return Results.BadRequest($"Heslo musí mať aspoň {PasswordPolicy.MinLength} znakov.");
                    case PasswordChangeStatus.SameAsCurrent:
                        return Results.BadRequest("Nové heslo sa musí líšiť od súčasného.");
                    default:
                        return Results.BadRequest("Súčasné heslo nie je správne.");
                }
            })
            .RequireAuthorization(AuthPolicies.PasswordChange)
            .WithName("ChangePassword");

        return endpoints;
    }

    private static Task SignInAsync(HttpContext httpContext, AppUser user) =>
        httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, ClaimsFactory.Create(user));

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    public sealed record LoginRequest(string? UserName, string? Password);

    public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);

    public sealed record UserInfoResponse(Guid Id, string UserName, string Role, bool MustChangePassword)
    {
        public static UserInfoResponse From(AppUser user) =>
            new(user.Id, user.UserName, user.Role.ToString(), user.MustChangePassword);
    }
}
