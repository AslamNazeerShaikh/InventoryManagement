using System.ComponentModel.DataAnnotations;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.DTOs;

/// <summary>Read-only projection of a single stock-movement ledger row.</summary>
public class StockMovementDto
{
    /// <summary>Movement identifier.</summary>
    public int Id { get; set; }

    /// <summary>Affected inventory identifier.</summary>
    public int InventoryId { get; set; }

    /// <summary>Affected item name.</summary>
    public string ItemName { get; set; } = string.Empty;

    /// <summary>Movement classification.</summary>
    public StockMovementType MovementType { get; set; }

    /// <summary>Signed quantity change applied by this movement.</summary>
    public int QuantityChange { get; set; }

    /// <summary>Available quantity immediately after this movement.</summary>
    public int BalanceAfter { get; set; }

    /// <summary>Optional reason.</summary>
    public string? Reason { get; set; }

    /// <summary>Optional notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Optional unit cost captured at receipt.</summary>
    public decimal? UnitCost { get; set; }

    /// <summary>Name of the user who performed the movement, if known.</summary>
    public string? PerformedByUserName { get; set; }

    /// <summary>Related assignment identifier, if the movement came from an assign/return.</summary>
    public int? AssignmentId { get; set; }

    /// <summary>Source location name for a transfer, if any.</summary>
    public string? FromLocationName { get; set; }

    /// <summary>Destination location name for a transfer, if any.</summary>
    public string? ToLocationName { get; set; }

    /// <summary>Supplier name for a receipt, if any.</summary>
    public string? SupplierName { get; set; }

    /// <summary>UTC timestamp when the movement was recorded.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>Payload for receiving/restocking stock into an item.</summary>
public class ReceiveStockDto
{
    /// <summary>Quantity received (≥1).</summary>
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    /// <summary>Optional unit cost of the received stock (non-negative).</summary>
    [Range(0, double.MaxValue)]
    public decimal? UnitCost { get; set; }

    /// <summary>Optional managed supplier the stock was received from.</summary>
    [Range(1, int.MaxValue)]
    public int? SupplierId { get; set; }

    /// <summary>Optional reason/reference (e.g. purchase order number, ≤500 chars).</summary>
    [StringLength(500)]
    public string? Reason { get; set; }

    /// <summary>Optional notes (≤1000 chars).</summary>
    [StringLength(1000)]
    public string? Notes { get; set; }
}

/// <summary>Payload for adjusting stock counts up or down (stock take / correction).</summary>
public class AdjustStockDto
{
    /// <summary>
    /// Signed change to apply to the counts (positive to add, negative to remove). Must be non-zero;
    /// the resulting available quantity cannot go below zero.
    /// </summary>
    [Range(-1_000_000, 1_000_000)]
    public int QuantityDelta { get; set; }

    /// <summary>Required reason for the adjustment (auditability, ≤500 chars).</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(500, MinimumLength = 1)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>Optional notes (≤1000 chars).</summary>
    [StringLength(1000)]
    public string? Notes { get; set; }
}

/// <summary>Payload for disposing of stock (permanent removal from circulation).</summary>
public class DisposeStockDto
{
    /// <summary>Quantity to dispose of (≥1).</summary>
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    /// <summary>Required reason for disposal (auditability, ≤500 chars).</summary>
    [Required(AllowEmptyStrings = false)]
    [StringLength(500, MinimumLength = 1)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>Optional notes (≤1000 chars).</summary>
    [StringLength(1000)]
    public string? Notes { get; set; }
}

/// <summary>Payload for transferring an item to a different managed location.</summary>
public class TransferStockDto
{
    /// <summary>Destination location (required).</summary>
    [Range(1, int.MaxValue)]
    public int ToLocationId { get; set; }

    /// <summary>Optional reason/reference (≤500 chars).</summary>
    [StringLength(500)]
    public string? Reason { get; set; }

    /// <summary>Optional notes (≤1000 chars).</summary>
    [StringLength(1000)]
    public string? Notes { get; set; }
}
