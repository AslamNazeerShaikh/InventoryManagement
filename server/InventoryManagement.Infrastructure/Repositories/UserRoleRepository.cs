using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Interfaces;
using InventoryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories;

/// <summary>EF Core repository for <see cref="UserRole"/> assignments.</summary>
public class UserRoleRepository : GenericRepository<UserRole>, IUserRoleRepository
{
    /// <summary>Creates the repository.</summary>
    public UserRoleRepository(AppDbContext appDbContext)
        : base(appDbContext) { }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Role>> GetRolesWithPermissionsForUserAsync(
        int userId,
        CancellationToken cancellationToken = default
    ) =>
        // Query roles the user is a member of and eagerly load their permission grants. Returning
        // entities (no projection) so the Include graph is honoured; all sets share the tenant filter.
        await DbContext
            .Set<Role>()
            .AsNoTracking()
            .Where(r => r.UserRoles.Any(ur => ur.UserId == userId))
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserRole>> GetForUserAsync(
        int userId,
        CancellationToken cancellationToken = default
    ) =>
        // Tracked: the caller reconciles (adds/removes) the user's assignments.
        await EntitySet.Where(ur => ur.UserId == userId).ToListAsync(cancellationToken).ConfigureAwait(false);
}
