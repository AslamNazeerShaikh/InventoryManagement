using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.DTOs;

namespace InventoryManagement.Domain.Interfaces;

/// <summary>Maintenance/calibration schedule operations (CRUD, completion and due tracking).</summary>
public interface IMaintenanceService
{
    /// <summary>Lists schedules for a single item, soonest-due first.</summary>
    Task<Result<IEnumerable<MaintenanceScheduleDto>>> GetByInventoryIdAsync(
        int inventoryId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists open schedules due within the given number of days (or already overdue).</summary>
    Task<Result<IEnumerable<MaintenanceScheduleDto>>> GetDueAsync(
        int daysAhead = 30,
        CancellationToken cancellationToken = default
    );

    /// <summary>Returns a deterministic page of all schedules, soonest-due first.</summary>
    Task<Result<PagedResult<MaintenanceScheduleDto>>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default
    );

    /// <summary>Gets a schedule by identifier.</summary>
    Task<Result<MaintenanceScheduleDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default
    );

    /// <summary>Creates a maintenance schedule for an item.</summary>
    Task<Result<MaintenanceScheduleDto>> CreateAsync(
        CreateMaintenanceScheduleDto createDto,
        CancellationToken cancellationToken = default
    );

    /// <summary>Updates a maintenance schedule.</summary>
    Task<Result<MaintenanceScheduleDto>> UpdateAsync(
        int id,
        UpdateMaintenanceScheduleDto updateDto,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Marks a schedule complete. When the schedule recurs (has an interval) or an explicit next-due
    /// date is supplied, the due date rolls forward and the schedule stays open; otherwise it closes.
    /// </summary>
    Task<Result<MaintenanceScheduleDto>> CompleteAsync(
        int id,
        CompleteMaintenanceDto completeDto,
        int performedByUserId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Soft-deletes a maintenance schedule.</summary>
    Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
