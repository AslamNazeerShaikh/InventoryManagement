namespace InventoryManagement.Domain.Enums;

/// <summary>
/// Condition an item is in when it is returned from an assignment. Recorded for audit and to drive
/// follow-up actions (e.g. flag damaged stock for maintenance). Generic across industries.
/// </summary>
public enum ReturnCondition
{
    /// <summary>Returned in good, reusable condition.</summary>
    Good = 1,

    /// <summary>Returned damaged.</summary>
    Damaged = 2,

    /// <summary>Reported lost / not physically returned.</summary>
    Lost = 3,

    /// <summary>Returned functional but requiring service before reuse.</summary>
    NeedsRepair = 4,
}
