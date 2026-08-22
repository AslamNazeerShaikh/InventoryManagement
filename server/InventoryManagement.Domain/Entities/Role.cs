using InventoryManagement.Domain.Common;

namespace InventoryManagement.Domain.Entities;

/// <summary>
/// A named, tenant-scoped collection of permissions that can be assigned to users. Roles are managed
/// dynamically per tenant; <see cref="IsSystem"/> roles are seeded defaults protected from deletion.
/// </summary>
public class Role : BaseEntity
{
    /// <summary>Unique (per tenant) role name, e.g. "Administrator" or a tenant-defined role.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional human-readable description.</summary>
    public string? Description { get; set; }

    /// <summary>Seeded system roles cannot be renamed or deleted by tenants.</summary>
    public bool IsSystem { get; set; }

    /// <summary>Permissions granted to this role.</summary>
    public virtual ICollection<RolePermission> RolePermissions { get; set; } =
        new List<RolePermission>();

    /// <summary>User memberships of this role.</summary>
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
