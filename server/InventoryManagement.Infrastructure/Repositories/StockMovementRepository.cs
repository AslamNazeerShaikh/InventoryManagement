using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Interfaces;
using InventoryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories;

/// <summary>EF Core repository for the append-only <see cref="StockMovement"/> ledger.</summary>
public class StockMovementRepository
    : GenericRepository<StockMovement>,
        IStockMovementRepository
{
    /// <summary>Creates the repository.</summary>
    public StockMovementRepository(AppDbContext appDbContext)
        : base(appDbContext) { }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StockMovement>> GetByInventoryIdAsync(
        int inventoryId,
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AsNoTracking()
            .Where(m => m.InventoryId == inventoryId)
            .Include(m => m.PerformedByUser)
            .Include(m => m.FromLocation)
            .Include(m => m.ToLocation)
            .Include(m => m.Supplier)
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
