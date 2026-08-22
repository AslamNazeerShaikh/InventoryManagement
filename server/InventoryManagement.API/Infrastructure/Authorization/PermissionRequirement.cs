using Microsoft.AspNetCore.Authorization;

namespace InventoryManagement.API.Infrastructure.Authorization;

/// <summary>
/// Authorization requirement satisfied when the authenticated principal carries a matching
/// permission claim. Backs the permission-based authorization model.
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    /// <summary>Creates the requirement for the given permission code.</summary>
    public PermissionRequirement(string permission) => Permission = permission;

    /// <summary>The required permission code, e.g. "inventory.manage".</summary>
    public string Permission { get; }
}
