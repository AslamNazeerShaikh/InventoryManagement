using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.DTOs;

namespace InventoryManagement.Domain.Interfaces;

/// <summary>
/// Stock operations that change an item's quantity or location and record an auditable
/// <see cref="Entities.StockMovement"/> ledger row for every change. All mutations are transactional
/// and rely on the entity concurrency token to prevent lost updates / oversell under concurrency.
/// </summary>
public interface IStockService
{
    /// <summary>Receives/restocks stock into an item (increases total and available quantity).</summary>
    Task<Result<InventoryDto>> ReceiveStockAsync(
        int inventoryId,
        ReceiveStockDto receiveDto,
        int performedByUserId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Adjusts an item's counts up or down (stock take / correction) with a required reason.</summary>
    Task<Result<InventoryDto>> AdjustStockAsync(
        int inventoryId,
        AdjustStockDto adjustDto,
        int performedByUserId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Disposes of available stock (permanent removal from circulation).</summary>
    Task<Result<InventoryDto>> DisposeStockAsync(
        int inventoryId,
        DisposeStockDto disposeDto,
        int performedByUserId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Transfers an item to a different managed location.</summary>
    Task<Result<InventoryDto>> TransferStockAsync(
        int inventoryId,
        TransferStockDto transferDto,
        int performedByUserId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Returns the full movement ledger for a single item, newest first.</summary>
    Task<Result<IEnumerable<StockMovementDto>>> GetMovementsAsync(
        int inventoryId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Returns a page of recent movements across all items, newest first.</summary>
    Task<Result<PagedResult<StockMovementDto>>> GetRecentMovementsAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default
    );
}
