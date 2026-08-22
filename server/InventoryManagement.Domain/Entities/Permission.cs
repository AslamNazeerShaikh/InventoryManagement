using InventoryManagement.Domain.Common;

namespace InventoryManagement.Domain.Entities;

/// <summary>
/// A tenant-scoped permission that can be granted to roles. Seeded from the application's permission
/// catalog and extensible per tenant for client-side feature gating. The <see cref="Code"/> is the
/// stable value checked by authorization.
/// </summary>
public class Permission : BaseEntity
{
    /// <summary>Stable machine code checked by authorization, e.g. "inventory.manage".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Optional human-readable description.</summary>
    public string? Description { get; set; }

    /// <summary>Optional UI grouping/category, e.g. "Inventory".</summary>
    public string? Category { get; set; }

    /// <summary>Roles that include this permission.</summary>
    public virtual ICollection<RolePermission> RolePermissions { get; set; } =
        new List<RolePermission>();
}
