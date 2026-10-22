namespace KrosExtension.Application.Configuration;

public interface IDataReadServerSecretMigrator
{
    /// <summary>Encrypts legacy plaintext SQL passwords. Returns the number of servers re-saved.</summary>
    Task<int> EncryptPlaintextSecretsAsync(CancellationToken ct = default);
}
