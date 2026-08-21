using System.ComponentModel.DataAnnotations;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.DTOs;

/// <summary>Read-only assignment projection returned by the API.</summary>
public class InventoryAssignmentDto
{
    /// <summary>Assignment identifier.</summary>
    public int Id { get; set; }

    /// <summary>Assigned inventory identifier.</summary>
    public int InventoryId { get; set; }

    /// <summary>Assigned equipment name.</summary>
    public string EquipmentName { get; set; } = string.Empty;

    /// <summary>Assigned item category.</summary>
    public string? Category { get; set; }

    /// <summary>Assigned item barcode.</summary>
    public string? Barcode { get; set; }

    /// <summary>Recipient user identifier.</summary>
    public int UserId { get; set; }

    /// <summary>Recipient user name.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Recipient user email.</summary>
    public string UserEmail { get; set; } = string.Empty;

    /// <summary>Quantity assigned.</summary>
    public int AssignedQuantity { get; set; }

    /// <summary>Quantity returned so far (supports partial returns).</summary>
    public int ReturnedQuantity { get; set; }

    /// <summary>Quantity still outstanding (assigned minus returned).</summary>
    public int OutstandingQuantity { get; set; }

    /// <summary>Number of times the expected return date has been renewed.</summary>
    public int RenewalCount { get; set; }

    /// <summary>Condition recorded at (last) return, if captured.</summary>
    public ReturnCondition? ReturnCondition { get; set; }

    /// <summary>UTC assignment timestamp.</summary>
    public DateTime AssignedDate { get; set; }

    /// <summary>UTC return timestamp, if returned.</summary>
    public DateTime? ReturnDate { get; set; }

    /// <summary>Expected return date, if set.</summary>
    public DateTime? ExpectedReturnDate { get; set; }

    /// <summary>Assignment status.</summary>
    public AssignmentStatus Status { get; set; }

    /// <summary>Notes captured at assignment time.</summary>
    public string? AssignmentNotes { get; set; }

    /// <summary>Notes captured at return time.</summary>
    public string? ReturnNotes { get; set; }

    /// <summary>Name of the user who created the assignment.</summary>
    public string? AssignedByUserName { get; set; }

    /// <summary>Name of the user who processed the return.</summary>
    public string? ReturnedToUserName { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>Payload for creating an assignment.</summary>
public class CreateInventoryAssignmentDto
{
    /// <summary>Inventory item to assign (required).</summary>
    [Range(1, int.MaxValue)]
    public int InventoryId { get; set; }

    /// <summary>Recipient user (required).</summary>
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }

    /// <summary>Quantity to assign (≥1).</summary>
    [Range(1, int.MaxValue)]
    public int AssignedQuantity { get; set; } = 1;

    /// <summary>Optional expected return date.</summary>
    public DateTime? ExpectedReturnDate { get; set; }

    /// <summary>Optional assignment notes (≤1000 chars).</summary>
    [StringLength(1000)]
    public string? AssignmentNotes { get; set; }
}

/// <summary>Payload for returning an assignment (supports partial returns).</summary>
public class ReturnInventoryAssignmentDto
{
    /// <summary>Assignment to return (required).</summary>
    [Range(1, int.MaxValue)]
    public int AssignmentId { get; set; }

    /// <summary>
    /// Optional quantity to return. When omitted, the full outstanding quantity is returned. When
    /// less than the outstanding quantity, the assignment stays active with the remainder still out.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? ReturnQuantity { get; set; }

    /// <summary>Optional condition the item is returned in.</summary>
    [EnumDataType(typeof(ReturnCondition))]
    public ReturnCondition? ReturnCondition { get; set; }

    /// <summary>Optional return notes (≤1000 chars).</summary>
    [StringLength(1000)]
    public string? ReturnNotes { get; set; }
}

/// <summary>Payload for renewing/extending an active assignment's expected return date.</summary>
public class RenewInventoryAssignmentDto
{
    /// <summary>Assignment to renew (required).</summary>
    [Range(1, int.MaxValue)]
    public int AssignmentId { get; set; }

    /// <summary>New expected return date (required; must be in the future).</summary>
    [Required]
    public DateTime NewExpectedReturnDate { get; set; }

    /// <summary>Optional notes about the renewal (≤1000 chars).</summary>
    [StringLength(1000)]
    public string? Notes { get; set; }
}

/// <summary>Payload for updating an active assignment.</summary>
public class UpdateInventoryAssignmentDto
{
    /// <summary>New assigned quantity (≥1).</summary>
    [Range(1, int.MaxValue)]
    public int AssignedQuantity { get; set; }

    /// <summary>Optional expected return date.</summary>
    public DateTime? ExpectedReturnDate { get; set; }

    /// <summary>New status.</summary>
    [EnumDataType(typeof(AssignmentStatus))]
    public AssignmentStatus Status { get; set; }

    /// <summary>Optional assignment notes (≤1000 chars).</summary>
    [StringLength(1000)]
    public string? AssignmentNotes { get; set; }
}

/// <summary>Assignment history for a single inventory item.</summary>
public class AssignmentHistoryDto
{
    /// <summary>Inventory identifier.</summary>
    public int InventoryId { get; set; }

    /// <summary>Equipment name.</summary>
    public string EquipmentName { get; set; } = string.Empty;

    /// <summary>Chronological assignment records.</summary>
    public List<InventoryAssignmentDto> Assignments { get; set; } = new();
}
