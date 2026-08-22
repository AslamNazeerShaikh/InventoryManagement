using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.Entities;

/// <summary>
/// An append-only ledger row capturing a single change to an <see cref="Inventory"/> item's stock or
/// location. Movements are never edited or deleted after creation; together they form the complete,
/// auditable lifecycle of an item (received, assigned, returned, adjusted, transferred, disposed).
/// The design is domain-agnostic — it works for any SKU in any industry.
/// </summary>
public class StockMovement : BaseEntity
{
    /// <summary>Foreign key to the affected inventory item.</summary>
    public int InventoryId { get; set; }

    /// <summary>Classification of this movement.</summary>
    public StockMovementType MovementType { get; set; }

    /// <summary>
    /// Signed change applied by this movement (positive for receipts/returns, negative for
    /// assignments/disposals, zero for pure location transfers).
    /// </summary>
    public int QuantityChange { get; set; }

    /// <summary>Available quantity of the item immediately after this movement (running balance).</summary>
    public int BalanceAfter { get; set; }

    /// <summary>Optional human-readable reason (e.g. purchase order, damage report, stock count).</summary>
    public string? Reason { get; set; }

    /// <summary>Optional free-text notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Optional unit cost captured at receipt time (for valuation).</summary>
    public decimal? UnitCost { get; set; }

    /// <summary>Foreign key to the user who performed the movement (nullable).</summary>
    public int? PerformedByUserId { get; set; }

    /// <summary>Foreign key to the related assignment when the movement was caused by an assign/return.</summary>
    public int? AssignmentId { get; set; }

    /// <summary>Source location for a transfer (nullable).</summary>
    public int? FromLocationId { get; set; }

    /// <summary>Destination location for a transfer (nullable).</summary>
    public int? ToLocationId { get; set; }

    /// <summary>Supplier associated with a receipt (nullable).</summary>
    public int? SupplierId { get; set; }

    /// <summary>Navigation to the affected inventory item (required).</summary>
    public virtual Inventory Inventory { get; set; } = null!;

    /// <summary>Navigation to the user who performed the movement.</summary>
    public virtual User? PerformedByUser { get; set; }

    /// <summary>Navigation to the related assignment.</summary>
    public virtual InventoryAssignment? Assignment { get; set; }

    /// <summary>Navigation to the source location.</summary>
    public virtual Location? FromLocation { get; set; }

    /// <summary>Navigation to the destination location.</summary>
    public virtual Location? ToLocation { get; set; }

    /// <summary>Navigation to the associated supplier.</summary>
    public virtual Supplier? Supplier { get; set; }
}
