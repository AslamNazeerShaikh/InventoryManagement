using System.ComponentModel.DataAnnotations;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.DTOs;

/// <summary>Read-only projection of a maintenance/calibration schedule.</summary>
public class MaintenanceScheduleDto
{
    /// <summary>Schedule identifier.</summary>
    public int Id { get; set; }

    /// <summary>Maintained asset identifier.</summary>
    public int InventoryId { get; set; }

    /// <summary>Maintained asset name.</summary>
    public string EquipmentName { get; set; } = string.Empty;

    /// <summary>Kind of service.</summary>
    public MaintenanceType MaintenanceType { get; set; }

    /// <summary>Schedule title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Optional recurrence interval in days.</summary>
    public int? IntervalDays { get; set; }

    /// <summary>UTC timestamp when the service was last performed.</summary>
    public DateTime? LastPerformedAt { get; set; }

    /// <summary>UTC timestamp when the service is next due.</summary>
    public DateTime NextDueAt { get; set; }

    /// <summary>Lifecycle status.</summary>
    public MaintenanceStatus Status { get; set; }

    /// <summary>Name of the user who last performed the service, if known.</summary>
    public string? PerformedByUserName { get; set; }

    /// <summary>Optional notes.</summary>
    public string? Notes { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>Payload for creating a maintenance schedule.</summary>
public class CreateMaintenanceScheduleDto
{
    /// <summary>Asset to schedule maintenance for (required).</summary>
    [Range(1, int.MaxValue)]
    public int InventoryId { get; set; }

    /// <summary>Kind of service.</summary>
    [EnumDataType(typeof(MaintenanceType))]
    public MaintenanceType MaintenanceType { get; set; } = MaintenanceType.Inspection;

    /// <summary>Schedule title (required, 1–200 chars).</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional description (≤1000 chars).</summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>Optional recurrence interval in days (≥1).</summary>
    [Range(1, int.MaxValue)]
    public int? IntervalDays { get; set; }

    /// <summary>When the service is first due (required).</summary>
    [Required]
    public DateTime NextDueAt { get; set; }

    /// <summary>Optional notes (≤1000 chars).</summary>
    [StringLength(1000)]
    public string? Notes { get; set; }
}

/// <summary>Payload for updating a maintenance schedule.</summary>
public class UpdateMaintenanceScheduleDto
{
    /// <summary>Kind of service.</summary>
    [EnumDataType(typeof(MaintenanceType))]
    public MaintenanceType MaintenanceType { get; set; }

    /// <summary>Schedule title (required, 1–200 chars).</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional description (≤1000 chars).</summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>Optional recurrence interval in days (≥1).</summary>
    [Range(1, int.MaxValue)]
    public int? IntervalDays { get; set; }

    /// <summary>When the service is next due (required).</summary>
    [Required]
    public DateTime NextDueAt { get; set; }

    /// <summary>Lifecycle status.</summary>
    [EnumDataType(typeof(MaintenanceStatus))]
    public MaintenanceStatus Status { get; set; }

    /// <summary>Optional notes (≤1000 chars).</summary>
    [StringLength(1000)]
    public string? Notes { get; set; }
}

/// <summary>Payload for marking a maintenance occurrence complete.</summary>
public class CompleteMaintenanceDto
{
    /// <summary>UTC timestamp the service was performed (defaults to now when omitted).</summary>
    public DateTime? PerformedAt { get; set; }

    /// <summary>
    /// Optional explicit next-due date. When omitted and the schedule has an interval, the next
    /// due date is rolled forward from <see cref="PerformedAt"/> by that interval.
    /// </summary>
    public DateTime? NextDueAt { get; set; }

    /// <summary>Optional completion notes (≤1000 chars).</summary>
    [StringLength(1000)]
    public string? Notes { get; set; }
}
