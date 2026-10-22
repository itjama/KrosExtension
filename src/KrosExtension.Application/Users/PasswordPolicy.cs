namespace KrosExtension.Application.Users;

public static class PasswordPolicy
{
    public const int MinLength = 10;

    public static bool IsLongEnough(string password) => password is not null && password.Length >= MinLength;
}
