using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.Interfaces;

/// <summary>Repository for <see cref="Inventory"/> aggregates with catalogue-oriented queries.</summary>
public interface IInventoryRepository : IGenericRepository<Inventory>
{
    /// <summary>Finds an inventory item by barcode, or <c>null</c>.</summary>
    Task<Inventory?> GetByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists available items expiring on or before <paramref name="beforeDate"/>, ordered by expiry.</summary>
    Task<IReadOnlyList<Inventory>> GetExpiringInventoriesAsync(
        DateTime beforeDate,
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists available items with remaining stock, ordered by name.</summary>
    Task<IReadOnlyList<Inventory>> GetAvailableInventoriesAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists items in the given status, ordered by name.</summary>
    Task<IReadOnlyList<Inventory>> GetInventoriesByStatusAsync(
        InventoryStatus status,
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists items in the given category, ordered by name.</summary>
    Task<IReadOnlyList<Inventory>> GetInventoriesByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default
    );

    /// <summary>Searches items by a free-text term across key fields, ordered by name (SQL-side).</summary>
    Task<IReadOnlyList<Inventory>> SearchInventoriesAsync(
        string searchTerm,
        CancellationToken cancellationToken = default
    );

    /// <summary>Returns whether an item with the given barcode exists.</summary>
    Task<bool> IsBarcodeExistsAsync(string barcode, CancellationToken cancellationToken = default);

    /// <summary>Returns whether an item with the given serial number exists.</summary>
    Task<bool> IsSerialNumberExistsAsync(
        string serialNumber,
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists available items at or below the low-stock threshold, ordered by remaining quantity.</summary>
    Task<IReadOnlyList<Inventory>> GetLowStockInventoriesAsync(
        int threshold = 5,
        CancellationToken cancellationToken = default
    );
}
