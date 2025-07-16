using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.Entities;

public class Inventory : BaseEntity
{
    public string EquipmentName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? Barcode { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string? Supplier { get; set; }
    public int Quantity { get; set; } = 1;
    public int AvailableQuantity { get; set; } = 1;
    public InventoryStatus Status { get; set; } = InventoryStatus.Available;
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public bool IsExpiryAlertSent { get; set; } = false;
    public int? CreatedByUserId { get; set; }

    // Navigation properties
    public virtual User? CreatedByUser { get; set; }
    public virtual ICollection<InventoryAssignment> Assignments { get; set; } =
        new List<InventoryAssignment>();
}
