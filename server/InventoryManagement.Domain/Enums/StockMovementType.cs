namespace InventoryManagement.Domain.Enums;

/// <summary>
/// Classifies a single row in the stock-movement ledger. Every quantity- or location-changing
/// operation records exactly one movement so an item's full lifecycle can be audited. The values
/// are deliberately generic (any industry / any SKU), not domain-specific.
/// </summary>
public enum StockMovementType
{
    /// <summary>New stock received/restocked (increases total and available quantity).</summary>
    Received = 1,

    /// <summary>Stock allocated to a recipient (decreases available quantity).</summary>
    Assigned = 2,

    /// <summary>Previously assigned stock returned (increases available quantity).</summary>
    Returned = 3,

    /// <summary>Manual correction of counts (increases or decreases quantity with a reason).</summary>
    Adjusted = 4,

    /// <summary>Stock moved between locations (no net quantity change).</summary>
    Transferred = 5,

    /// <summary>Stock permanently removed from circulation (decreases total and available quantity).</summary>
    Disposed = 6,
}
