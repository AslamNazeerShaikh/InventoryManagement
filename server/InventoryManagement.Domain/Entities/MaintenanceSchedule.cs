using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.Entities;

/// <summary>
/// A planned or recurring service occurrence for a durable <see cref="Inventory"/> asset (inspection,
/// calibration, service, repair or cleaning). When completed with an <see cref="IntervalDays"/> set,
/// the next due date is rolled forward automatically. Domain-agnostic across industries.
/// </summary>
public class MaintenanceSchedule : BaseEntity
{
    /// <summary>Foreign key to the asset being maintained.</summary>
    public int InventoryId { get; set; }

    /// <summary>Kind of service.</summary>
    public MaintenanceType MaintenanceType { get; set; }

    /// <summary>Short title for the schedule (e.g. "Annual calibration").</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional detailed description of the work.</summary>
    public string? Description { get; set; }

    /// <summary>Optional recurrence interval in days; when set, completing rolls the due date forward.</summary>
    public int? IntervalDays { get; set; }

    /// <summary>UTC timestamp when the service was last performed, if ever.</summary>
    public DateTime? LastPerformedAt { get; set; }

    /// <summary>UTC timestamp when the service is next due.</summary>
    public DateTime NextDueAt { get; set; }

    /// <summary>Lifecycle status of the schedule.</summary>
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Scheduled;

    /// <summary>Foreign key to the user who last performed the service (nullable).</summary>
    public int? PerformedByUserId { get; set; }

    /// <summary>Optional operational notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Navigation to the maintained asset (required).</summary>
    public virtual Inventory Inventory { get; set; } = null!;

    /// <summary>Navigation to the user who last performed the service.</summary>
    public virtual User? PerformedByUser { get; set; }
}
