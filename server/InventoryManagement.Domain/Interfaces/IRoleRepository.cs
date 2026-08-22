using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Domain.Interfaces;

/// <summary>Repository for <see cref="Role"/> aggregates and their permission grants.</summary>
public interface IRoleRepository : IGenericRepository<Role>
{
    /// <summary>Gets a role by name within the current tenant, or <c>null</c>.</summary>
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Gets a role (tracked) with its <c>RolePermissions.Permission</c> graph for mutation.</summary>
    Task<Role?> GetWithPermissionsAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Lists all roles with their permission codes (read-only), ordered by name.</summary>
    Task<IReadOnlyList<Role>> ListWithPermissionsAsync(
        CancellationToken cancellationToken = default
    );
}
