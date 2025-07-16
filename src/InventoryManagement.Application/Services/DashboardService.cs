using InventoryManagement.Application.Mapping;
using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Domain.Interfaces;

namespace InventoryManagement.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<DashboardStatsDto>> GetDashboardStatsAsync()
    {
        try
        {
            var stats = new DashboardStatsDto
            {
                TotalInventories = await _unitOfWork.Inventories.CountAsync(),
                AvailableInventories = await _unitOfWork.Inventories.CountAsync(x =>
                    x.Status == InventoryStatus.Available
                ),
                AssignedInventories = await _unitOfWork.Inventories.CountAsync(x =>
                    x.Status == InventoryStatus.Assigned
                ),
                TotalUsers = await _unitOfWork.Users.CountAsync(),
                ActiveAssignments = await _unitOfWork.InventoryAssignments.CountAsync(x =>
                    x.Status == AssignmentStatus.Active
                ),
            };

            // Calculate expiring inventories (3 months)
            var expiryDate = DateTime.UtcNow.AddMonths(
                BusinessConstants.ExpiryAlert.DefaultMonthsBefore
            );
            stats.ExpiringInventories = await _unitOfWork.Inventories.CountAsync(x =>
                x.ExpiryDate.HasValue
                && x.ExpiryDate <= expiryDate
                && x.Status == InventoryStatus.Available
            );

            // Calculate low stock inventories
            stats.LowStockInventories = await _unitOfWork.Inventories.CountAsync(x =>
                x.AvailableQuantity <= BusinessConstants.Inventory.LowStockThreshold
                && x.Status == InventoryStatus.Available
            );

            // Calculate overdue assignments
            var today = DateTime.UtcNow.Date;
            stats.OverdueAssignments = await _unitOfWork.InventoryAssignments.CountAsync(x =>
                x.Status == AssignmentStatus.Active
                && x.ExpectedReturnDate.HasValue
                && x.ExpectedReturnDate.Value.Date < today
            );

            return ApiResponse<DashboardStatsDto>.Success(stats);
        }
        catch (Exception ex)
        {
            return ApiResponse<DashboardStatsDto>.Failure(
                $"Error retrieving dashboard stats: {ex.Message}"
            );
        }
    }

    public async Task<ApiResponse<IEnumerable<InventoryDto>>> GetRecentInventoriesAsync(
        int count = 10
    )
    {
        try
        {
            var recentInventories = await _unitOfWork.Inventories.GetAllAsync(x => x.CreatedByUser);
            var sortedInventories = recentInventories
                .OrderByDescending(x => x.CreatedAt)
                .Take(count);

            var inventoryDtos = sortedInventories.ToDto();
            return ApiResponse<IEnumerable<InventoryDto>>.Success(inventoryDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<InventoryDto>>.Failure(
                $"Error retrieving recent inventories: {ex.Message}"
            );
        }
    }

    public async Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>> GetRecentAssignmentsAsync(
        int count = 10
    )
    {
        try
        {
            var recentAssignments = await _unitOfWork.InventoryAssignments.GetAllAsync(
                x => x.Inventory,
                x => x.User,
                x => x.AssignedByUser
            );

            var sortedAssignments = recentAssignments
                .OrderByDescending(x => x.AssignedDate)
                .Take(count);

            var assignmentDtos = sortedAssignments.ToDto();
            return ApiResponse<IEnumerable<InventoryAssignmentDto>>.Success(assignmentDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<InventoryAssignmentDto>>.Failure(
                $"Error retrieving recent assignments: {ex.Message}"
            );
        }
    }

    public async Task<ApiResponse<IEnumerable<InventoryDto>>> GetExpiryAlertsAsync()
    {
        try
        {
            var expiryDate = DateTime.UtcNow.AddMonths(
                BusinessConstants.ExpiryAlert.DefaultMonthsBefore
            );
            var expiringInventories = await _unitOfWork.Inventories.GetExpiringInventoriesAsync(
                expiryDate
            );
            var inventoryDtos = expiringInventories.ToDto();
            return ApiResponse<IEnumerable<InventoryDto>>.Success(inventoryDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<InventoryDto>>.Failure(
                $"Error retrieving expiry alerts: {ex.Message}"
            );
        }
    }

    public async Task<ApiResponse<IEnumerable<InventoryDto>>> GetLowStockAlertsAsync()
    {
        try
        {
            var lowStockInventories = await _unitOfWork.Inventories.GetLowStockInventoriesAsync();
            var inventoryDtos = lowStockInventories.ToDto();
            return ApiResponse<IEnumerable<InventoryDto>>.Success(inventoryDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<InventoryDto>>.Failure(
                $"Error retrieving low stock alerts: {ex.Message}"
            );
        }
    }

    public async Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>> GetOverdueAlertsAsync()
    {
        try
        {
            var overdueAssignments =
                await _unitOfWork.InventoryAssignments.GetOverdueAssignmentsAsync();
            var assignmentDtos = overdueAssignments.ToDto();
            return ApiResponse<IEnumerable<InventoryAssignmentDto>>.Success(assignmentDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<InventoryAssignmentDto>>.Failure(
                $"Error retrieving overdue alerts: {ex.Message}"
            );
        }
    }
}
