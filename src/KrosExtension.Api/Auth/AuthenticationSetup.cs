using KrosExtension.Domain.Users;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace KrosExtension.Api.Auth;

public static class AuthenticationSetup
{
    public const string CookieName = "LedgerPulse.Auth";
    public const string LoginRateLimitPolicy = "login";

    public static IServiceCollection AddAppAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));

        var keysPath = configuration["DataProtection:KeysPath"];
        if (string.IsNullOrWhiteSpace(keysPath))
        {
            keysPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "KrosExtension", "keys");
        }

        var dataProtection = services.AddDataProtection()
            .SetApplicationName("KrosExtension")
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        if (OperatingSystem.IsWindows())
        {
            dataProtection.ProtectKeysWithDpapi(protectToLocalMachine: true);
        }

        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = CookieName;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
                options.Events.OnValidatePrincipal = CookieSessionValidator.ValidateAsync;
            });

        services
            .AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<IOptions<AuthOptions>>((cookie, auth) => cookie.Cookie.SecurePolicy = auth.Value.CookieSecurePolicy);

        // Full access: signed in and not forced to change the password first.
        var fullAccess = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireAssertion(context =>
                !context.User.HasClaim(claim => claim.Type == AppClaimTypes.MustChangePassword))
            .Build();

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(fullAccess)
            .AddPolicy(AuthPolicies.Admin, policy => policy
                .Combine(fullAccess)
                .RequireRole(nameof(UserRole.Admin)))
            .AddPolicy(AuthPolicies.PasswordChange, policy => policy.RequireAuthenticatedUser());

        // Default trust is loopback only, which matches the reverse proxy running on the same machine.
        services.Configure<ForwardedHeadersOptions>(options =>
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor);

        var loginLimit = configuration.GetValue<int?>($"{AuthOptions.SectionName}:{nameof(AuthOptions.LoginRateLimitPerMinute)}")
            ?? new AuthOptions().LoginRateLimitPerMinute;
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(LoginRateLimitPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = loginLimit,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                    }));
        });

        return services;
    }

    public static WebApplication UseAppAuthentication(this WebApplication app)
    {
        app.UseForwardedHeaders();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}
