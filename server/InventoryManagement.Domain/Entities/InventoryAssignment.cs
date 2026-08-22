using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.Entities;

/// <summary>
/// Records the allocation of a quantity of an <see cref="Inventory"/> item to a <see cref="User"/>,
/// including who assigned it, expected/actual return dates and the return handler.
/// </summary>
public class InventoryAssignment : BaseEntity
{
    /// <summary>Foreign key to the assigned inventory item.</summary>
    public int InventoryId { get; set; }

    /// <summary>Foreign key to the user the item is assigned to.</summary>
    public int UserId { get; set; }

    /// <summary>Quantity allocated by this assignment.</summary>
    public int AssignedQuantity { get; set; } = 1;

    /// <summary>UTC timestamp when the item was assigned.</summary>
    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

    /// <summary>UTC timestamp when the item was returned, or <c>null</c> if still out.</summary>
    public DateTime? ReturnDate { get; set; }

    /// <summary>Optional expected return date; used to compute overdue assignments.</summary>
    public DateTime? ExpectedReturnDate { get; set; }

    /// <summary>Current status of the assignment.</summary>
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Active;

    /// <summary>Optional notes captured at assignment time.</summary>
    public string? AssignmentNotes { get; set; }

    /// <summary>Optional notes captured at return time.</summary>
    public string? ReturnNotes { get; set; }

    /// <summary>Foreign key to the user who performed the assignment (nullable).</summary>
    public int? AssignedByUserId { get; set; }

    /// <summary>Foreign key to the user who processed the return (nullable).</summary>
    public int? ReturnedToUserId { get; set; }

    /// <summary>
    /// Quantity already returned so far. Supports partial returns: while
    /// <c>0 &lt; ReturnedQuantity &lt; AssignedQuantity</c> the assignment stays
    /// <see cref="AssignmentStatus.Active"/>; it becomes <see cref="AssignmentStatus.Returned"/>
    /// only when the full quantity has been returned.
    /// </summary>
    public int ReturnedQuantity { get; set; }

    /// <summary>Number of times the assignment's expected return date has been renewed/extended.</summary>
    public int RenewalCount { get; set; }

    /// <summary>Condition recorded when the item was (last) returned, if captured.</summary>
    public ReturnCondition? ReturnCondition { get; set; }

    /// <summary>Navigation to the assigned inventory item (required).</summary>
    public virtual Inventory Inventory { get; set; } = null!;

    /// <summary>Navigation to the recipient user (required).</summary>
    public virtual User User { get; set; } = null!;

    /// <summary>Navigation to the user who created the assignment.</summary>
    public virtual User? AssignedByUser { get; set; }

    /// <summary>Navigation to the user who processed the return.</summary>
    public virtual User? ReturnedToUser { get; set; }
}
