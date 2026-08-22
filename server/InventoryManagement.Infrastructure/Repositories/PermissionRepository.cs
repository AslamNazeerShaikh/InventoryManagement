using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Interfaces;
using InventoryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories;

/// <summary>EF Core repository for the tenant <see cref="Permission"/> catalog.</summary>
public class PermissionRepository : GenericRepository<Permission>, IPermissionRepository
{
    /// <summary>Creates the repository.</summary>
    public PermissionRepository(AppDbContext appDbContext)
        : base(appDbContext) { }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Permission>> ListCatalogAsync(
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AsNoTracking()
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Permission>> GetByCodesAsync(
        IEnumerable<string> codes,
        CancellationToken cancellationToken = default
    )
    {
        var wanted = codes.Distinct().ToArray();
        return await EntitySet
            .Where(p => wanted.Contains(p.Code))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
