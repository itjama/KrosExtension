using System.Security.Claims;
using KrosExtension.Application.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace KrosExtension.Api.Auth;

public static class CookieSessionValidator
{
    /// <summary>
    /// Rejects the cookie when the user no longer exists, is blocked or its security stamp has changed
    /// (password change or reset, role change, block).
    /// </summary>
    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        var idValue = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var stamp = principal?.FindFirstValue(AppClaimTypes.SecurityStamp);

        if (Guid.TryParse(idValue, out var userId) && !string.IsNullOrEmpty(stamp))
        {
            var accounts = context.HttpContext.RequestServices.GetRequiredService<IUserAccountService>();
            var user = await accounts.FindByIdAsync(userId, context.HttpContext.RequestAborted);

            if (user is not null && !user.IsBlocked && string.Equals(user.SecurityStamp, stamp, StringComparison.Ordinal))
            {
                return;
            }
        }

        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
