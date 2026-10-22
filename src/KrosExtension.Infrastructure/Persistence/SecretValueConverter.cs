using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KrosExtension.Infrastructure.Persistence;

public sealed class SecretValueConverter(IDataProtector protector)
    : ValueConverter<string, string>(
        value => Encrypt(protector, value),
        value => Decrypt(protector, value))
{
    public const string Purpose = "KrosExtension.DataReadServerSecrets";

    public const string Prefix = "enc:";

    private static string Encrypt(IDataProtector protector, string value) =>
        string.IsNullOrEmpty(value) ? value : Prefix + protector.Protect(value);

    private static string Decrypt(IDataProtector protector, string value) =>
        !string.IsNullOrEmpty(value) && value.StartsWith(Prefix, StringComparison.Ordinal)
            ? protector.Unprotect(value[Prefix.Length..])
            : value;
}
