using InventoryManagement.Domain.DTOs;

namespace InventoryManagement.Domain.Interfaces;

public interface IInventoryService
{
    Task<ApiResponse<IEnumerable<InventoryDto>>> GetAllInventoriesAsync();
    Task<ApiResponse<InventoryDto>> GetInventoryByIdAsync(int id);
    Task<ApiResponse<InventoryDto>> GetInventoryByBarcodeAsync(string barcode);
    Task<ApiResponse<InventoryDto>> CreateInventoryAsync(
        CreateInventoryDto createInventoryDto,
        int createdByUserId
    );
    Task<ApiResponse<InventoryDto>> UpdateInventoryAsync(
        int id,
        UpdateInventoryDto updateInventoryDto
    );
    Task<ApiResponse<bool>> DeleteInventoryAsync(int id);
    Task<ApiResponse<IEnumerable<InventoryDto>>> GetAvailableInventoriesAsync();
    Task<ApiResponse<IEnumerable<InventoryDto>>> GetExpiringInventoriesAsync(int monthsBefore = 3);
    Task<ApiResponse<IEnumerable<InventoryDto>>> GetLowStockInventoriesAsync(int threshold = 5);
    Task<ApiResponse<IEnumerable<InventoryDto>>> SearchInventoriesAsync(
        InventorySearchDto searchDto
    );
    Task<ApiResponse<PagedResult<InventoryDto>>> GetInventoriesPagedAsync(
        int pageNumber,
        int pageSize
    );
    Task<ApiResponse<IEnumerable<InventoryDto>>> GetInventoriesByCategoryAsync(string category);
}
