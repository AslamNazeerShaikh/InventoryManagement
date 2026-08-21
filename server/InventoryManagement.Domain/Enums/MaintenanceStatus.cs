namespace InventoryManagement.Domain.Enums;

/// <summary>Lifecycle status of a <see cref="Entities.MaintenanceSchedule"/> occurrence.</summary>
public enum MaintenanceStatus
{
    /// <summary>Planned with a future due date.</summary>
    Scheduled = 1,

    /// <summary>Due now or within the configured reminder window.</summary>
    Due = 2,

    /// <summary>Past its due date and not yet completed.</summary>
    Overdue = 3,

    /// <summary>Completed; a follow-up occurrence may have been generated from the interval.</summary>
    Completed = 4,

    /// <summary>Cancelled and no longer tracked.</summary>
    Cancelled = 5,
}
