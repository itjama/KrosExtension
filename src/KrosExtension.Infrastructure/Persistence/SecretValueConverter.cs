using System.Security.Cryptography;
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

    private static string Decrypt(IDataProtector protector, string value)
    {
        if (string.IsNullOrEmpty(value) || !value.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return value;
        }

        try
        {
            return protector.Unprotect(value[Prefix.Length..]);
        }
        catch (CryptographicException)
        {
            // The key ring cannot decrypt this value (keys lost/changed or DB restored elsewhere).
            // The ciphertext is unrecoverable; treating it as "no credentials" keeps the app usable
            // so an admin can delete/re-add the server instead of every load throwing.
            return string.Empty;
        }
    }
}
