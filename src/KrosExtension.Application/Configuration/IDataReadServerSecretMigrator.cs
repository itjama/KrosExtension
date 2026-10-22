namespace KrosExtension.Application.Configuration;

public interface IDataReadServerSecretMigrator
{
    /// <summary>Encrypts legacy plaintext SQL passwords. Returns the number of servers re-saved.</summary>
    Task<int> EncryptPlaintextSecretsAsync(CancellationToken ct = default);

    /// <summary>
    /// Finds stored encrypted SQL passwords that the current key ring cannot decrypt
    /// (keys lost or the database moved to another machine). Never returns the values.
    /// </summary>
    Task<IReadOnlyList<UndecryptableSecret>> FindUndecryptableSecretsAsync(CancellationToken ct = default);
}

/// <summary>An encrypted secret that cannot be decrypted: the server id and the column name only.</summary>
public sealed record UndecryptableSecret(Guid ServerId, string Column);
