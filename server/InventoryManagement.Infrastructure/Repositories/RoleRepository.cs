using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Interfaces;
using InventoryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories;

/// <summary>EF Core repository for <see cref="Role"/> with permission-graph loading.</summary>
public class RoleRepository : GenericRepository<Role>, IRoleRepository
{
    /// <summary>Creates the repository.</summary>
    public RoleRepository(AppDbContext appDbContext)
        : base(appDbContext) { }

    /// <inheritdoc />
    public async Task<Role?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default
    ) => await EntitySet.FirstOrDefaultAsync(r => r.Name == name, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Role?> GetWithPermissionsAsync(
        int id,
        CancellationToken cancellationToken = default
    ) =>
        // Tracked: the caller reconciles the role's permission grants.
        await EntitySet
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Role>> ListWithPermissionsAsync(
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AsNoTracking()
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
