using InventoryManagement.Application.Mapping;
using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Application.Services;

/// <summary>
/// Dashboard aggregation service. Uses a single scoped unit of work sequentially (never sharing the
/// context across concurrent operations) and pushes ordering/limits into SQL for efficiency.
/// </summary>
public sealed class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>Creates the dashboard service.</summary>
    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<ApiResponse<DashboardStatsDto>> GetDashboardStatsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var now = DateTime.UtcNow;
        var expiryThreshold = now.AddMonths(BusinessConstants.ExpiryAlert.DefaultMonthsBefore);
        var today = now.Date;

        // Sequential counts against the shared scoped context (no concurrent DbContext use).
        var stats = new DashboardStatsDto
        {
            TotalInventories = await _unitOfWork
                .Inventories.CountAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false),
            AvailableInventories = await _unitOfWork
                .Inventories.CountAsync(
                    x => x.Status == InventoryStatus.Available,
                    cancellationToken
                )
                .ConfigureAwait(false),
            AssignedInventories = await _unitOfWork
                .Inventories.CountAsync(
                    x => x.Status == InventoryStatus.Assigned,
                    cancellationToken
                )
                .ConfigureAwait(false),
            TotalUsers = await _unitOfWork
                .Users.CountAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false),
            ActiveAssignments = await _unitOfWork
                .InventoryAssignments.CountAsync(
                    x => x.Status == AssignmentStatus.Active,
                    cancellationToken
                )
                .ConfigureAwait(false),
            ExpiringInventories = await _unitOfWork
                .Inventories.CountAsync(
                    x =>
                        x.ExpiryDate.HasValue
                        && x.ExpiryDate <= expiryThreshold
                        && x.Status == InventoryStatus.Available,
                    cancellationToken
                )
                .ConfigureAwait(false),
            LowStockInventories = await _unitOfWork
                .Inventories.CountAsync(
                    x =>
                        x.AvailableQuantity <= BusinessConstants.Inventory.LowStockThreshold
                        && x.Status == InventoryStatus.Available,
                    cancellationToken
                )
                .ConfigureAwait(false),
            OverdueAssignments = await _unitOfWork
                .InventoryAssignments.CountAsync(
                    x =>
                        x.Status == AssignmentStatus.Active
                        && x.ExpectedReturnDate.HasValue
                        && x.ExpectedReturnDate.Value.Date < today,
                    cancellationToken
                )
                .ConfigureAwait(false),
        };

        return ApiResponse<DashboardStatsDto>.Success(stats);
    }

    /// <inheritdoc />
    public async Task<ApiResponse<IEnumerable<InventoryDto>>> GetRecentInventoriesAsync(
        int count = 10,
        CancellationToken cancellationToken = default
    )
    {
        count = Math.Clamp(count, 1, 50);
        var inventories = await _unitOfWork
            .Inventories.ListAsync(
                include: q => q.Include(x => x.CreatedByUser),
                orderBy: q => q.OrderByDescending(x => x.CreatedAt),
                take: count,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        return ApiResponse<IEnumerable<InventoryDto>>.Success(inventories.ToDto());
    }

    /// <inheritdoc />
    public async Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>> GetRecentAssignmentsAsync(
        int count = 10,
        CancellationToken cancellationToken = default
    )
    {
        count = Math.Clamp(count, 1, 50);
        var assignments = await _unitOfWork
            .InventoryAssignments.ListAsync(
                include: q =>
                    q.Include(x => x.Inventory).Include(x => x.User).Include(x => x.AssignedByUser),
                orderBy: q => q.OrderByDescending(x => x.AssignedDate),
                take: count,
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        return ApiResponse<IEnumerable<InventoryAssignmentDto>>.Success(assignments.ToDto());
    }

    /// <inheritdoc />
    public async Task<ApiResponse<IEnumerable<InventoryDto>>> GetExpiryAlertsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var expiryDate = DateTime.UtcNow.AddMonths(
            BusinessConstants.ExpiryAlert.DefaultMonthsBefore
        );
        var expiring = await _unitOfWork
            .Inventories.GetExpiringInventoriesAsync(expiryDate, cancellationToken)
            .ConfigureAwait(false);
        return ApiResponse<IEnumerable<InventoryDto>>.Success(expiring.ToDto());
    }

    /// <inheritdoc />
    public async Task<ApiResponse<IEnumerable<InventoryDto>>> GetLowStockAlertsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var lowStock = await _unitOfWork
            .Inventories.GetLowStockInventoriesAsync(
                BusinessConstants.Inventory.LowStockThreshold,
                cancellationToken
            )
            .ConfigureAwait(false);
        return ApiResponse<IEnumerable<InventoryDto>>.Success(lowStock.ToDto());
    }

    /// <inheritdoc />
    public async Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>> GetOverdueAlertsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var overdue = await _unitOfWork
            .InventoryAssignments.GetOverdueAssignmentsAsync(cancellationToken)
            .ConfigureAwait(false);
        return ApiResponse<IEnumerable<InventoryAssignmentDto>>.Success(overdue.ToDto());
    }
}
