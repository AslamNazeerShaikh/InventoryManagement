namespace InventoryManagement.Domain.Common;

/// <summary>
/// Base type for every persisted entity. Provides identity, auditing, soft-delete markers
/// and an optimistic-concurrency token shared by all tables.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>Surrogate primary key (database-generated identity).</summary>
    public int Id { get; set; }

    /// <summary>
    /// Owning tenant. Every persisted row belongs to exactly one tenant; the persistence layer
    /// filters all queries by this value and stamps it on insert, giving row-level isolation for a
    /// multi-tenant / multi-client deployment. Left as <see cref="Guid.Empty"/> until stamped on
    /// save (or backfilled to the default tenant for pre-existing rows).
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>UTC timestamp captured when the row was first inserted.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp of the most recent update, or <c>null</c> if never modified.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Identifier (user name / system) that created the row.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Identifier (user name / system) that last modified the row.</summary>
    public string? UpdatedBy { get; set; }

    /// <summary>Soft-delete flag. Filtered out by global query filters when <c>true</c>.</summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>UTC timestamp when the row was soft-deleted.</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>Identifier (user name / system) that soft-deleted the row.</summary>
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Optimistic-concurrency stamp. Configured as a concurrency token and rotated on every
    /// insert/update by <c>AppDbContext</c>, giving provider-agnostic lost-update protection
    /// (works on SQLite where a native <c>rowversion</c> column is unavailable).
    /// </summary>
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
}
