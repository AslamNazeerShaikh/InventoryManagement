using InventoryManagement.Domain.Common;

namespace InventoryManagement.Domain.Entities;

/// <summary>Join entity assigning a <see cref="Role"/> to a <see cref="User"/> (many-to-many).</summary>
public class UserRole : BaseEntity
{
    /// <summary>Foreign key to the user.</summary>
    public int UserId { get; set; }

    /// <summary>Foreign key to the role.</summary>
    public int RoleId { get; set; }

    /// <summary>Navigation to the user.</summary>
    public virtual User User { get; set; } = null!;

    /// <summary>Navigation to the role.</summary>
    public virtual Role Role { get; set; } = null!;
}
