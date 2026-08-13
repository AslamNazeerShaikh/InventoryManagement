using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.DTOs;

namespace InventoryManagement.Domain.Interfaces;

/// <summary>Aggregated dashboard statistics and alert feeds.</summary>
public interface IDashboardService
{
    /// <summary>Computes headline counts in a single, consolidated database pass.</summary>
    Task<Result<DashboardStatsDto>> GetDashboardStatsAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists the most recently created inventory items (SQL-side ordering and limit).</summary>
    Task<Result<IEnumerable<InventoryDto>>> GetRecentInventoriesAsync(
        int count = 10,
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists the most recent assignments (SQL-side ordering and limit).</summary>
    Task<Result<IEnumerable<InventoryAssignmentDto>>> GetRecentAssignmentsAsync(
        int count = 10,
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists items approaching expiry.</summary>
    Task<Result<IEnumerable<InventoryDto>>> GetExpiryAlertsAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists items at or below the low-stock threshold.</summary>
    Task<Result<IEnumerable<InventoryDto>>> GetLowStockAlertsAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists overdue assignments.</summary>
    Task<Result<IEnumerable<InventoryAssignmentDto>>> GetOverdueAlertsAsync(
        CancellationToken cancellationToken = default
    );
}
