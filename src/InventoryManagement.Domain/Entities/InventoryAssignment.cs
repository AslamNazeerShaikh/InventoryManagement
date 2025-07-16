using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.Entities;

public class InventoryAssignment : BaseEntity
{
    public int InventoryId { get; set; }
    public int UserId { get; set; }
    public int AssignedQuantity { get; set; } = 1;
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ReturnDate { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Active;
    public string? AssignmentNotes { get; set; }
    public string? ReturnNotes { get; set; }
    public int? AssignedByUserId { get; set; }
    public int? ReturnedToUserId { get; set; }

    // Navigation properties
    public virtual Inventory Inventory { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual User? AssignedByUser { get; set; }
    public virtual User? ReturnedToUser { get; set; }
}
