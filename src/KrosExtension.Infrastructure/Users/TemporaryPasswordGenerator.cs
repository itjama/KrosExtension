using System.Security.Cryptography;

namespace KrosExtension.Infrastructure.Users;

public static class TemporaryPasswordGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
    private const int Length = 12;

    public static string Generate() => RandomNumberGenerator.GetString(Alphabet, Length);
}
