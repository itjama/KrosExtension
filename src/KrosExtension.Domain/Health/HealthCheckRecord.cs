using KrosExtension.Domain.Common;

namespace KrosExtension.Domain.Health;

public sealed class HealthCheckRecord : Entity
{
    private HealthCheckRecord()
    {
    }

    public HealthCheckRecord(DateTimeOffset checkedAt, string source)
    {
        CheckedAt = checkedAt;
        Source = source;
    }

    public DateTimeOffset CheckedAt { get; private set; }

    public string Source { get; private set; } = string.Empty;
}
