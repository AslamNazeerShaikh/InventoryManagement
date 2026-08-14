using InventoryManagement.Domain.Security;

namespace InventoryManagement.Infrastructure.Security;

/// <summary>Default <see cref="IDateTimeProvider"/> backed by the real system clock (UTC).</summary>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}
