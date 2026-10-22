namespace KrosExtension.Api.Auth;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public CookieSecurePolicy CookieSecurePolicy { get; set; } = CookieSecurePolicy.Always;

    public int LoginRateLimitPerMinute { get; set; } = 20;
}
