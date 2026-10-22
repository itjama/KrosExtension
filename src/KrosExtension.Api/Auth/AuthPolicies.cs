namespace KrosExtension.Api.Auth;

public static class AuthPolicies
{
    /// <summary>Authenticated admin who does not have to change the password.</summary>
    public const string Admin = "Admin";

    /// <summary>Any authenticated user, including one who still has to change the password.</summary>
    public const string PasswordChange = "PasswordChange";
}
