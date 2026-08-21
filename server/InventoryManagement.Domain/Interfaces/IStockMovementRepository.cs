using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Domain.Interfaces;

/// <summary>
/// Repository for the append-only <see cref="StockMovement"/> ledger. Movements are only ever
/// inserted (never updated or deleted), so this exposes insert plus read-oriented queries.
/// </summary>
public interface IStockMovementRepository : IGenericRepository<StockMovement>
{
    /// <summary>
    /// Lists the movement history for a single item, newest first, with the related actor, location
    /// and supplier navigations loaded for projection.
    /// </summary>
    Task<IReadOnlyList<StockMovement>> GetByInventoryIdAsync(
        int inventoryId,
        CancellationToken cancellationToken = default
    );
}
