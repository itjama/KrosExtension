namespace KrosExtension.Application.Companies;

public interface IOlympDashboardReader
{
    Task<IReadOnlyList<OlympDashboardCompany>> GetCompaniesAsync(
        CancellationToken cancellationToken = default);
}
