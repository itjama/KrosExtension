namespace KrosExtension.Application.Companies;

public interface IOmegaDashboardReader
{
    Task<IReadOnlyList<OmegaDashboardCompany>> GetCompaniesAsync(
        CancellationToken cancellationToken = default);
}
