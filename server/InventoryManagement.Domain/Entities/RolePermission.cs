using InventoryManagement.Domain.Common;

namespace InventoryManagement.Domain.Entities;

/// <summary>Join entity granting a <see cref="Permission"/> to a <see cref="Role"/> (many-to-many).</summary>
public class RolePermission : BaseEntity
{
    /// <summary>Foreign key to the role.</summary>
    public int RoleId { get; set; }

    /// <summary>Foreign key to the permission.</summary>
    public int PermissionId { get; set; }

    /// <summary>Navigation to the role.</summary>
    public virtual Role Role { get; set; } = null!;

    /// <summary>Navigation to the permission.</summary>
    public virtual Permission Permission { get; set; } = null!;
}
