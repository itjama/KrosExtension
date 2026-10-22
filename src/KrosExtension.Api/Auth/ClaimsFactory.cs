using System.Security.Claims;
using KrosExtension.Domain.Users;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace KrosExtension.Api.Auth;

public static class AppClaimTypes
{
    public const string SecurityStamp = "security_stamp";
    public const string MustChangePassword = "must_change_password";
}

public static class ClaimsFactory
{
    public static ClaimsPrincipal Create(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(AppClaimTypes.SecurityStamp, user.SecurityStamp),
        };

        if (user.MustChangePassword)
        {
            claims.Add(new Claim(AppClaimTypes.MustChangePassword, "true"));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
}
