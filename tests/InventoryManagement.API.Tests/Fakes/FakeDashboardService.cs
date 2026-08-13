using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Interfaces;

namespace InventoryManagement.API.Tests.Fakes;

/// <summary>
/// Hand-rolled fake of <see cref="IDashboardService"/> that records arguments
/// and returns canned successful responses, avoiding a mocking-library dependency.
/// </summary>
public sealed class FakeDashboardService : IDashboardService
{
    public int? LastRecentInventoriesCount { get; private set; }

    public DashboardStatsDto StatsToReturn { get; set; } = new() { TotalInventories = 7 };

    public Task<ApiResponse<DashboardStatsDto>> GetDashboardStatsAsync() =>
        Task.FromResult(ApiResponse<DashboardStatsDto>.Success(StatsToReturn));

    public Task<ApiResponse<IEnumerable<InventoryDto>>> GetRecentInventoriesAsync(int count = 10)
    {
        LastRecentInventoriesCount = count;
        return Task.FromResult(
            ApiResponse<IEnumerable<InventoryDto>>.Success(new List<InventoryDto>())
        );
    }

    public Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>> GetRecentAssignmentsAsync(
        int count = 10
    ) =>
        Task.FromResult(
            ApiResponse<IEnumerable<InventoryAssignmentDto>>.Success(
                new List<InventoryAssignmentDto>()
            )
        );

    public Task<ApiResponse<IEnumerable<InventoryDto>>> GetExpiryAlertsAsync() =>
        Task.FromResult(
            ApiResponse<IEnumerable<InventoryDto>>.Success(new List<InventoryDto>())
        );

    public Task<ApiResponse<IEnumerable<InventoryDto>>> GetLowStockAlertsAsync() =>
        Task.FromResult(
            ApiResponse<IEnumerable<InventoryDto>>.Success(new List<InventoryDto>())
        );

    public Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>> GetOverdueAlertsAsync() =>
        Task.FromResult(
            ApiResponse<IEnumerable<InventoryAssignmentDto>>.Success(
                new List<InventoryAssignmentDto>()
            )
        );
}
