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

    /// <summary>
    /// Optional reorder point (par level). When set and <see cref="AvailableQuantity"/> falls to or
    /// below this value, the item is flagged for reordering. Null disables reorder tracking.
    /// </summary>
    public int? ReorderLevel { get; set; }

    /// <summary>Optional suggested quantity to order when a reorder is triggered.</summary>
    public int? ReorderQuantity { get; set; }

    /// <summary>
    /// Optional foreign key to a managed <see cref="Entities.Supplier"/>. Additive to the legacy
    /// free-text <see cref="Supplier"/> string, which is retained for backward compatibility.
    /// </summary>
    public int? SupplierId { get; set; }

    /// <summary>
    /// Optional foreign key to a managed <see cref="Entities.Location"/> (the item's current primary
    /// location). Additive to the legacy free-text <see cref="Location"/> string, retained for
    /// backward compatibility.
    /// </summary>
    public int? LocationId { get; set; }

    /// <summary>Whether an expiry alert has already been dispatched (prevents duplicates).</summary>
    public bool IsExpiryAlertSent { get; set; } = false;

    /// <summary>Foreign key to the creating user (nullable; set to null if that user is removed).</summary>
    public int? CreatedByUserId { get; set; }

    /// <summary>Navigation to the creating user.</summary>
    public virtual User? CreatedByUser { get; set; }

    /// <summary>Navigation to the managed supplier (see <see cref="SupplierId"/>).</summary>
    public virtual Supplier? SupplierEntity { get; set; }

    /// <summary>Navigation to the managed storage location (see <see cref="LocationId"/>).</summary>
    public virtual Location? LocationEntity { get; set; }

    /// <summary>Assignment history for this item.</summary>
    public virtual ICollection<InventoryAssignment> Assignments { get; set; } =
        new List<InventoryAssignment>();

    /// <summary>Append-only stock-movement ledger for this item.</summary>
    public virtual ICollection<StockMovement> StockMovements { get; set; } =
        new List<StockMovement>();

    /// <summary>Maintenance/calibration schedules for this item.</summary>
    public virtual ICollection<MaintenanceSchedule> MaintenanceSchedules { get; set; } =
        new List<MaintenanceSchedule>();
}
