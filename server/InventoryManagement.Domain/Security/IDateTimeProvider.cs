namespace InventoryManagement.Domain.Security;

/// <summary>
/// Abstraction over the system clock. Injecting time removes hidden static dependencies on
/// <see cref="DateTime.UtcNow"/>, making time-dependent logic (token expiry, overdue calculations,
/// idempotency windows) deterministically testable.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>The current UTC instant.</summary>
    DateTime UtcNow { get; }
}
