using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.DTOs;

namespace InventoryManagement.Domain.Interfaces;

/// <summary>
/// Dynamic, tenant-scoped role and permission administration. Tenants compose the application's
/// permission catalog into custom roles and assign roles to users; system roles are protected.
/// </summary>
public interface IRoleService
{
    /// <summary>Lists the tenant's roles with their granted permission codes.</summary>
    Task<Result<IEnumerable<RoleDto>>> GetRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets a single role with its granted permission codes.</summary>
    Task<Result<RoleDto>> GetRoleByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Returns the assignable permission catalog for the tenant.</summary>
    Task<Result<IEnumerable<PermissionDto>>> GetPermissionCatalogAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>Creates a new custom role with the supplied permission grants.</summary>
    Task<Result<RoleDto>> CreateRoleAsync(
        CreateRoleDto createRoleDto,
        CancellationToken cancellationToken = default
    );

    /// <summary>Updates a role's name, description and permission set (system roles: permissions only).</summary>
    Task<Result<RoleDto>> UpdateRoleAsync(
        int id,
        UpdateRoleDto updateRoleDto,
        CancellationToken cancellationToken = default
    );

    /// <summary>Deletes a custom (non-system) role that has no remaining members.</summary>
    Task<Result<bool>> DeleteRoleAsync(int id, CancellationToken cancellationToken = default);
}
