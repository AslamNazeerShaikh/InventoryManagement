using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Domain.DTOs;

/// <summary>Read-only projection of an assignable permission from the tenant's catalog.</summary>
public class PermissionDto
{
    /// <summary>Stable machine code, e.g. "inventory.manage".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Human-readable description.</summary>
    public string? Description { get; set; }

    /// <summary>UI grouping/category.</summary>
    public string? Category { get; set; }
}

/// <summary>Read-only projection of a role and the permission codes it grants.</summary>
public class RoleDto
{
    /// <summary>Role identifier.</summary>
    public int Id { get; set; }

    /// <summary>Role name (unique within the tenant).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Whether this is a protected seeded system role.</summary>
    public bool IsSystem { get; set; }

    /// <summary>Granted permission codes.</summary>
    public List<string> Permissions { get; set; } = new();
}

/// <summary>Payload for creating a role.</summary>
public class CreateRoleDto
{
    /// <summary>Role name (required, unique per tenant, 1–100 chars).</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description (≤500 chars).</summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>Permission codes to grant to the role.</summary>
    public List<string> Permissions { get; set; } = new();
}

/// <summary>Payload for updating a role's name, description and permission set.</summary>
public class UpdateRoleDto
{
    /// <summary>Role name (required, unique per tenant, 1–100 chars).</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description (≤500 chars).</summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>The full set of permission codes the role should grant (replaces the current set).</summary>
    public List<string> Permissions { get; set; } = new();
}
