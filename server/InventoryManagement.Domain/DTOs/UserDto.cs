using System.ComponentModel.DataAnnotations;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.DTOs;

/// <summary>Read-only user projection returned by the API (never exposes password/token material).</summary>
public class UserDto
{
    /// <summary>User identifier.</summary>
    public int Id { get; set; }

    /// <summary>Owning tenant identifier (multi-tenant isolation boundary).</summary>
    public Guid TenantId { get; set; }

    /// <summary>Display name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Login email.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Whether the user has administrative privileges.</summary>
    public bool IsAdmin { get; set; }

    /// <summary>Whether the user is a clinical provider.</summary>
    public bool IsProvider { get; set; }

    /// <summary>Coarse-grained role.</summary>
    public UserRole Role { get; set; }

    /// <summary>Whether the account is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>UTC timestamp of last login.</summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>Payload for creating a new user.</summary>
public class CreateUserDto
{
    /// <summary>Display name (required, 1–100 chars).</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Login email (required, valid address, ≤256 chars).</summary>
    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Initial password (required, 8–128 chars).</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    /// <summary>Whether to grant administrative privileges.</summary>
    public bool IsAdmin { get; set; }

    /// <summary>Whether the user is a clinical provider.</summary>
    public bool IsProvider { get; set; }

    /// <summary>Assigned role.</summary>
    [EnumDataType(typeof(UserRole))]
    public UserRole Role { get; set; } = UserRole.Staff;
}

/// <summary>Payload for updating an existing user's profile.</summary>
public class UpdateUserDto
{
    /// <summary>Display name (required, 1–100 chars).</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Login email (required, valid address, ≤256 chars).</summary>
    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Whether the user has administrative privileges.</summary>
    public bool IsAdmin { get; set; }

    /// <summary>Whether the user is a clinical provider.</summary>
    public bool IsProvider { get; set; }

    /// <summary>Assigned role.</summary>
    [EnumDataType(typeof(UserRole))]
    public UserRole Role { get; set; }

    /// <summary>Whether the account is active.</summary>
    public bool IsActive { get; set; }
}

/// <summary>Login credentials.</summary>
public class LoginDto
{
    /// <summary>Login email (required, valid address).</summary>
    [Required(AllowEmptyStrings = false)]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Password (required).</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(128, MinimumLength = 1)]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Payload for changing the current user's password.</summary>
public class ChangePasswordDto
{
    /// <summary>The user's current password.</summary>
    [Required(AllowEmptyStrings = false)]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>The desired new password (8–128 chars).</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(128, MinimumLength = 8)]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>Confirmation of the new password (must match <see cref="NewPassword"/>).</summary>
    [Required(AllowEmptyStrings = false)]
    [Compare(nameof(NewPassword), ErrorMessage = "New password and confirmation do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
