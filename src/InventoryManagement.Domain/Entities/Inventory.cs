using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.Entities;

/// <summary>
/// A trackable piece of medical equipment/stock. <see cref="Quantity"/> is the total owned and
/// <see cref="AvailableQuantity"/> the amount not currently assigned; the invariant
/// <c>0 &lt;= AvailableQuantity &lt;= Quantity</c> is enforced by the application layer.
/// </summary>
public class Inventory : BaseEntity
{
    /// <summary>Human-readable equipment name.</summary>
    public string EquipmentName { get; set; } = string.Empty;

    /// <summary>Optional free-text description.</summary>
    public string? Description { get; set; }

    /// <summary>Optional classification/category.</summary>
    public string? Category { get; set; }

    /// <summary>Optional manufacturer brand.</summary>
    public string? Brand { get; set; }

    /// <summary>Optional model designation.</summary>
    public string? Model { get; set; }

    /// <summary>Optional unique serial number (unique among non-deleted rows when present).</summary>
    public string? SerialNumber { get; set; }

    /// <summary>Optional unique barcode (unique among non-deleted rows when present).</summary>
    public string? Barcode { get; set; }

    /// <summary>Optional expiry date; expired items cannot be assigned.</summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>Optional manufacture date.</summary>
    public DateTime? ManufactureDate { get; set; }

    /// <summary>Optional purchase price.</summary>
    public decimal? PurchasePrice { get; set; }

    /// <summary>Optional supplier name.</summary>
    public string? Supplier { get; set; }

    /// <summary>Total quantity owned.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Quantity currently available for assignment.</summary>
    public int AvailableQuantity { get; set; } = 1;

    /// <summary>Lifecycle status of the item.</summary>
    public InventoryStatus Status { get; set; } = InventoryStatus.Available;

    /// <summary>Optional physical storage location.</summary>
    public string? Location { get; set; }

    /// <summary>Optional operational notes.</summary>
    public string? Notes { get; set; }

    /// <summary>Whether an expiry alert has already been dispatched (prevents duplicates).</summary>
    public bool IsExpiryAlertSent { get; set; } = false;

    /// <summary>Foreign key to the creating user (nullable; set to null if that user is removed).</summary>
    public int? CreatedByUserId { get; set; }

    /// <summary>Navigation to the creating user.</summary>
    public virtual User? CreatedByUser { get; set; }

    /// <summary>Assignment history for this item.</summary>
    public virtual ICollection<InventoryAssignment> Assignments { get; set; } =
        new List<InventoryAssignment>();
}
