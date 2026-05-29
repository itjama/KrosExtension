using KrosExtension.Domain.Configuration;

namespace KrosExtension.Application.Configuration;

public sealed record ProductRefreshSettingsDto(
    ProductType ProductType,
    TimeOnly RefreshTime);
