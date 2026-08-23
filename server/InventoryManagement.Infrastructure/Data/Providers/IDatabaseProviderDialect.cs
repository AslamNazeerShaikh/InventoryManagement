using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Data.Providers;

/// <summary>
/// Narrow seam for the two places where persistence cannot stay provider-agnostic: recognizing a
/// store-enforced unique-constraint violation (error codes differ per provider) and expressing the
/// SQL predicate of a filtered "unique when present" index. One implementation per supported
/// provider keeps that knowledge in a single, unit-testable place instead of scattering
/// provider-specific <c>catch</c> blocks and SQL literals through the data layer.
/// </summary>
public interface IDatabaseProviderDialect
{
    /// <summary>EF Core provider name this dialect targets (for diagnostics and tests).</summary>
    string ProviderName { get; }

    /// <summary>
    /// Whether <paramref name="exception"/> was caused by a unique index/constraint violation, as
    /// opposed to any other store failure (foreign key, NOT NULL, check constraint, deadlock…).
    /// </summary>
    /// <param name="exception">The failure raised by <c>SaveChanges</c>.</param>
    bool IsUniqueConstraintViolation(DbUpdateException exception);

    /// <summary>
    /// Builds the SQL <c>WHERE</c> predicate for a unique index that must constrain only rows where
    /// <paramref name="valueColumn"/> actually holds a value and the row is not soft-deleted, or
    /// <c>null</c> when the provider is unknown and no filtered index can be emitted safely.
    /// </summary>
    /// <param name="valueColumn">Column whose value must be unique when present.</param>
    /// <param name="softDeleteColumn">Boolean soft-delete column that must be <c>false</c>.</param>
    string? BuildUniqueWhenPresentFilter(string valueColumn, string softDeleteColumn);
}
