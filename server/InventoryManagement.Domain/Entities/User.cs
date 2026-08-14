using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.Entities;

/// <summary>
/// Application user / operator. Holds authentication material and role flags that drive
/// authorization policies. Passwords and refresh tokens are stored only as one-way hashes.
/// </summary>
public class User : BaseEntity
{
    /// <summary>Display name of the user.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Unique login email (case-sensitive as stored; compared verbatim).</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>BCrypt hash of the user's password. Never store or return the plaintext.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Grants administrative privileges (full access).</summary>
    public bool IsAdmin { get; set; } = false;

    /// <summary>Marks the user as a clinical provider (elevated, non-admin privileges).</summary>
    public bool IsProvider { get; set; } = false;

    /// <summary>Coarse-grained role used for role-based policies.</summary>
    public UserRole Role { get; set; } = UserRole.Staff;

    /// <summary>When <c>false</c> the account is disabled and cannot authenticate.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>UTC timestamp of the last successful login.</summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// SHA-256 hash (Base64) of the currently issued refresh token. The raw token is only ever
    /// returned to the client once; the server persists the hash so a leaked database cannot be
    /// used to impersonate the user. Cleared on logout and password change.
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>UTC expiry of the current refresh token.</summary>
    public DateTime? RefreshTokenExpiryTime { get; set; }

    /// <summary>Assignments where this user is the recipient of the equipment.</summary>
    public virtual ICollection<InventoryAssignment> AssignedInventories { get; set; } =
        new List<InventoryAssignment>();

    /// <summary>Inventory items created by this user.</summary>
    public virtual ICollection<Inventory> CreatedInventories { get; set; } = new List<Inventory>();
}
