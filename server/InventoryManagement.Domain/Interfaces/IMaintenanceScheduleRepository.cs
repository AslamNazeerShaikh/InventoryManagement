using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Domain.Interfaces;

/// <summary>Repository for <see cref="MaintenanceSchedule"/> aggregates.</summary>
public interface IMaintenanceScheduleRepository : IGenericRepository<MaintenanceSchedule>
{
    /// <summary>
    /// Lists schedules that are open (not completed/cancelled) and due on or before
    /// <paramref name="dueBefore"/>, soonest first, with the item navigation loaded for projection.
    /// </summary>
    Task<IReadOnlyList<MaintenanceSchedule>> GetDueAsync(
        DateTime dueBefore,
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists all schedules for a single item, soonest-due first, with the item navigation loaded.</summary>
    Task<IReadOnlyList<MaintenanceSchedule>> GetByInventoryIdAsync(
        int inventoryId,
        CancellationToken cancellationToken = default
    );
}
