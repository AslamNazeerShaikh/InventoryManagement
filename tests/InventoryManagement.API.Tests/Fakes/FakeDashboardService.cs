using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Interfaces;

namespace InventoryManagement.API.Tests.Fakes;

/// <summary>
/// Hand-rolled fake of <see cref="IDashboardService"/> that records arguments and returns canned
/// successful results, avoiding a mocking-library dependency.
/// </summary>
public sealed class FakeDashboardService : IDashboardService
{
    /// <summary>The last <c>count</c> argument passed to <see cref="GetRecentInventoriesAsync"/>.</summary>
    public int? LastRecentInventoriesCount { get; private set; }

    /// <summary>Stats payload returned by <see cref="GetDashboardStatsAsync"/>.</summary>
    public DashboardStatsDto StatsToReturn { get; set; } = new() { TotalInventories = 7 };

    /// <inheritdoc />
    public Task<Result<DashboardStatsDto>> GetDashboardStatsAsync(
        CancellationToken cancellationToken = default
    ) => Task.FromResult(Result<DashboardStatsDto>.Success(StatsToReturn));

    /// <inheritdoc />
    public Task<Result<IEnumerable<InventoryDto>>> GetRecentInventoriesAsync(
        int count = 10,
        CancellationToken cancellationToken = default
    )
    {
        LastRecentInventoriesCount = count;
        return Task.FromResult(
            Result<IEnumerable<InventoryDto>>.Success(new List<InventoryDto>())
        );
    }

    /// <inheritdoc />
    public Task<Result<IEnumerable<InventoryAssignmentDto>>> GetRecentAssignmentsAsync(
        int count = 10,
        CancellationToken cancellationToken = default
    ) =>
        Task.FromResult(
            Result<IEnumerable<InventoryAssignmentDto>>.Success(new List<InventoryAssignmentDto>())
        );

    /// <inheritdoc />
    public Task<Result<IEnumerable<InventoryDto>>> GetExpiryAlertsAsync(
        CancellationToken cancellationToken = default
    ) => Task.FromResult(Result<IEnumerable<InventoryDto>>.Success(new List<InventoryDto>()));

    /// <inheritdoc />
    public Task<Result<IEnumerable<InventoryDto>>> GetLowStockAlertsAsync(
        CancellationToken cancellationToken = default
    ) => Task.FromResult(Result<IEnumerable<InventoryDto>>.Success(new List<InventoryDto>()));

    /// <inheritdoc />
    public Task<Result<IEnumerable<InventoryAssignmentDto>>> GetOverdueAlertsAsync(
        CancellationToken cancellationToken = default
    ) =>
        Task.FromResult(
            Result<IEnumerable<InventoryAssignmentDto>>.Success(new List<InventoryAssignmentDto>())
        );
}
