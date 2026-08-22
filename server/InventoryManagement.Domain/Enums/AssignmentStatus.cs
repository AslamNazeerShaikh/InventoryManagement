namespace InventoryManagement.Domain.Enums;

/// <summary>Status of an <see cref="Entities.InventoryAssignment"/>.</summary>
public enum AssignmentStatus
{
    /// <summary>Currently assigned and outstanding.</summary>
    Active = 1,

    /// <summary>Returned to stock.</summary>
    Returned = 2,

    /// <summary>Expired while assigned.</summary>
    Expired = 3,

    /// <summary>Reported lost.</summary>
    Lost = 4,

    /// <summary>Returned damaged.</summary>
    Damaged = 5,
}
