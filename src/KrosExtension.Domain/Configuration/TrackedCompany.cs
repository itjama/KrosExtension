using KrosExtension.Domain.Common;

namespace KrosExtension.Domain.Configuration;

public sealed class TrackedCompany : Entity
{
    private TrackedCompany()
    {
    }

    public TrackedCompany(
        ProductType productType,
        Guid dataReadServerId,
        string databaseName,
        string companyName,
        int? accountingYear = null)
    {
        ProductType = productType;
        DataReadServerId = dataReadServerId;
        DatabaseName = databaseName;
        CompanyName = companyName;
        AccountingYear = accountingYear;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public ProductType ProductType { get; private set; }

    public Guid DataReadServerId { get; private set; }

    public string DatabaseName { get; private set; } = string.Empty;

    public string CompanyName { get; private set; } = string.Empty;

    public int? AccountingYear { get; private set; }

    public bool IsTracked { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(string companyName, bool isTracked, int? accountingYear = null)
    {
        CompanyName = companyName;
        AccountingYear = accountingYear;
        IsTracked = isTracked;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
