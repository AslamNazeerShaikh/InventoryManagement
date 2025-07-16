using InventoryManagement.Domain.DTOs;

namespace InventoryManagement.Domain.Interfaces;

public interface IDashboardService
{
    Task<ApiResponse<DashboardStatsDto>> GetDashboardStatsAsync();
    Task<ApiResponse<IEnumerable<InventoryDto>>> GetRecentInventoriesAsync(int count = 10);
    Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>> GetRecentAssignmentsAsync(
        int count = 10
    );
    Task<ApiResponse<IEnumerable<InventoryDto>>> GetExpiryAlertsAsync();
    Task<ApiResponse<IEnumerable<InventoryDto>>> GetLowStockAlertsAsync();
    Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>> GetOverdueAlertsAsync();
}
