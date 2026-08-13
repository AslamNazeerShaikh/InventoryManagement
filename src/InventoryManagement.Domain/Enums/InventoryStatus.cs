namespace InventoryManagement.Domain.Enums;

/// <summary>Lifecycle status of an <see cref="Entities.Inventory"/> item.</summary>
public enum InventoryStatus
{
    /// <summary>In stock and assignable.</summary>
    Available = 1,

    /// <summary>Fully allocated; no available quantity remaining.</summary>
    Assigned = 2,

    /// <summary>Held/reserved and not currently assignable.</summary>
    Reserved = 3,

    /// <summary>Past its expiry date.</summary>
    Expired = 4,

    /// <summary>Damaged and withdrawn from circulation.</summary>
    Damaged = 5,

    /// <summary>Permanently disposed of.</summary>
    Disposed = 6,
}
