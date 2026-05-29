namespace KrosExtension.Application.Companies;

public interface IOlympCompanyDetailReader
{
    Task<OlympCompanyDetail?> GetCompanyDetailAsync(
        Guid dataReadServerId,
        string databaseName,
        CancellationToken cancellationToken = default);
}
