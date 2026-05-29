using KrosExtension.Domain.Common;

namespace KrosExtension.Domain.Configuration;

public sealed class ProductRefreshSettings : Entity
{
    private ProductRefreshSettings()
    {
    }

    public ProductRefreshSettings(ProductType productType, TimeOnly refreshTime)
    {
        ProductType = productType;
        RefreshTime = refreshTime;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public ProductType ProductType { get; private set; }

    public TimeOnly RefreshTime { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(TimeOnly refreshTime)
    {
        RefreshTime = refreshTime;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
