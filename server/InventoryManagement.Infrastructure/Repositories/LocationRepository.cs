using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Interfaces;
using InventoryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories;

/// <summary>EF Core repository for <see cref="Location"/>.</summary>
public class LocationRepository : GenericRepository<Location>, ILocationRepository
{
    /// <summary>Creates the repository.</summary>
    public LocationRepository(AppDbContext appDbContext)
        : base(appDbContext) { }

    /// <inheritdoc />
    public async Task<bool> IsCodeExistsAsync(
        string code,
        int? excludeId = null,
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AnyAsync(
                l => l.Code == code && (excludeId == null || l.Id != excludeId),
                cancellationToken
            )
            .ConfigureAwait(false);
}
