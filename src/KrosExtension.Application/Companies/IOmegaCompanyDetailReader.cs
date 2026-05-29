namespace KrosExtension.Application.Companies;

public interface IOmegaCompanyDetailReader
{
    Task<OmegaCompanyDetail?> GetCompanyDetailAsync(
        Guid dataReadServerId,
        string databaseName,
        CancellationToken cancellationToken = default);
}
