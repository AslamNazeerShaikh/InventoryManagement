using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Interfaces;
using InventoryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories;

/// <summary>EF Core repository for <see cref="Supplier"/>.</summary>
public class SupplierRepository : GenericRepository<Supplier>, ISupplierRepository
{
    /// <summary>Creates the repository.</summary>
    public SupplierRepository(AppDbContext appDbContext)
        : base(appDbContext) { }

    /// <inheritdoc />
    public async Task<bool> IsNameExistsAsync(
        string name,
        int? excludeId = null,
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AnyAsync(
                s => s.Name == name && (excludeId == null || s.Id != excludeId),
                cancellationToken
            )
            .ConfigureAwait(false);
}
