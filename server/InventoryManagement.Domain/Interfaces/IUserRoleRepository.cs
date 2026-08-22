using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Domain.Interfaces;

/// <summary>Repository for <see cref="UserRole"/> assignments (user ↔ role membership).</summary>
public interface IUserRoleRepository : IGenericRepository<UserRole>
{
    /// <summary>
    /// Gets the roles assigned to a user, each with its permission graph loaded, so a caller can
    /// resolve the user's distinct effective permission codes (e.g. for token issuance).
    /// </summary>
    Task<IReadOnlyList<Role>> GetRolesWithPermissionsForUserAsync(
        int userId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Gets the user's current role-assignment rows (tracked) so they can be reconciled.</summary>
    Task<IReadOnlyList<UserRole>> GetForUserAsync(
        int userId,
        CancellationToken cancellationToken = default
    );
}
