namespace KrosExtension.Application.Companies;

public interface IOmegaOrphanPaymentsReader
{
    Task<OmegaOrphanPaymentsResult?> GetOrphanPaymentsAsync(
        Guid dataReadServerId,
        string databaseName,
        CancellationToken cancellationToken = default);
}
