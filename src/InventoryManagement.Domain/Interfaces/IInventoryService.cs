using InventoryManagement.Domain.DTOs;

namespace InventoryManagement.Domain.Interfaces;

/// <summary>Inventory catalogue operations (CRUD, search, alerts and paging).</summary>
public interface IInventoryService
{
    /// <summary>Lists all inventory items.</summary>
    Task<ApiResponse<IEnumerable<InventoryDto>>> GetAllInventoriesAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>Gets an inventory item by identifier.</summary>
    Task<ApiResponse<InventoryDto>> GetInventoryByIdAsync(
        int id,
        CancellationToken cancellationToken = default
    );

    /// <summary>Gets an inventory item by barcode.</summary>
    Task<ApiResponse<InventoryDto>> GetInventoryByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken = default
    );

    /// <summary>Creates an inventory item owned by the specified user.</summary>
    Task<ApiResponse<InventoryDto>> CreateInventoryAsync(
        CreateInventoryDto createInventoryDto,
        int createdByUserId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Updates an inventory item, adjusting available quantity consistently.</summary>
    Task<ApiResponse<InventoryDto>> UpdateInventoryAsync(
        int id,
        UpdateInventoryDto updateInventoryDto,
        CancellationToken cancellationToken = default
    );

    /// <summary>Soft-deletes an inventory item that has no active assignments.</summary>
    Task<ApiResponse<bool>> DeleteInventoryAsync(
        int id,
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists available items with remaining stock.</summary>
    Task<ApiResponse<IEnumerable<InventoryDto>>> GetAvailableInventoriesAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists items expiring within the given number of months.</summary>
    Task<ApiResponse<IEnumerable<InventoryDto>>> GetExpiringInventoriesAsync(
        int monthsBefore = 3,
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists items at or below the given low-stock threshold.</summary>
    Task<ApiResponse<IEnumerable<InventoryDto>>> GetLowStockInventoriesAsync(
        int threshold = 5,
        CancellationToken cancellationToken = default
    );

    /// <summary>Searches items using the supplied criteria (executed SQL-side).</summary>
    Task<ApiResponse<IEnumerable<InventoryDto>>> SearchInventoriesAsync(
        InventorySearchDto searchDto,
        CancellationToken cancellationToken = default
    );

    /// <summary>Returns a deterministic page of items.</summary>
    Task<ApiResponse<PagedResult<InventoryDto>>> GetInventoriesPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists items in a given category.</summary>
    Task<ApiResponse<IEnumerable<InventoryDto>>> GetInventoriesByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default
    );
}
