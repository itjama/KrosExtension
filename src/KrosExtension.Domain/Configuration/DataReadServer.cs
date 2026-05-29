using KrosExtension.Domain.Common;

namespace KrosExtension.Domain.Configuration;

public sealed class DataReadServer : Entity
{
    private DataReadServer()
    {
    }

    public DataReadServer(
        string displayName,
        string serverName,
        ProductAuthenticationMode olympAuthenticationMode,
        string? olympUserName,
        string? olympPassword,
        ProductAuthenticationMode omegaAuthenticationMode,
        string? omegaUserName,
        string? omegaPassword,
        bool trustServerCertificate)
    {
        DisplayName = displayName;
        ServerName = serverName;
        OlympAuthenticationMode = olympAuthenticationMode;
        OlympUserName = olympUserName ?? string.Empty;
        OlympPassword = olympPassword ?? string.Empty;
        OmegaAuthenticationMode = omegaAuthenticationMode;
        OmegaUserName = omegaUserName ?? string.Empty;
        OmegaPassword = omegaPassword ?? string.Empty;
        TrustServerCertificate = trustServerCertificate;
    }

    public string DisplayName { get; private set; } = string.Empty;

    public string ServerName { get; private set; } = string.Empty;

    public ProductAuthenticationMode OlympAuthenticationMode { get; private set; } = ProductAuthenticationMode.Windows;

    public string OlympUserName { get; private set; } = string.Empty;

    public string OlympPassword { get; private set; } = string.Empty;

    public ProductAuthenticationMode OmegaAuthenticationMode { get; private set; } = ProductAuthenticationMode.Windows;

    public string OmegaUserName { get; private set; } = string.Empty;

    public string OmegaPassword { get; private set; } = string.Empty;

    public bool TrustServerCertificate { get; private set; }

    public bool IsActive { get; private set; } = true;

    public bool HasOlympCredentials =>
        !string.IsNullOrWhiteSpace(OlympUserName) && !string.IsNullOrWhiteSpace(OlympPassword);

    public bool HasOmegaCredentials =>
        !string.IsNullOrWhiteSpace(OmegaUserName) && !string.IsNullOrWhiteSpace(OmegaPassword);

    public bool HasOlympAccess =>
        OlympAuthenticationMode == ProductAuthenticationMode.Windows || HasOlympCredentials;

    public bool HasOmegaAccess =>
        OmegaAuthenticationMode == ProductAuthenticationMode.Windows || HasOmegaCredentials;
}
