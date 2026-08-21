namespace InventoryManagement.Domain.Enums;

/// <summary>
/// Kind of service performed on a durable asset. Generic across industries (calibrating a lab
/// instrument, servicing a vehicle, inspecting a fire extinguisher, etc.).
/// </summary>
public enum MaintenanceType
{
    /// <summary>Visual/functional inspection.</summary>
    Inspection = 1,

    /// <summary>Calibration against a reference standard.</summary>
    Calibration = 2,

    /// <summary>Routine preventive service.</summary>
    Service = 3,

    /// <summary>Corrective repair of a fault.</summary>
    Repair = 4,

    /// <summary>Cleaning / decontamination.</summary>
    Cleaning = 5,
}
