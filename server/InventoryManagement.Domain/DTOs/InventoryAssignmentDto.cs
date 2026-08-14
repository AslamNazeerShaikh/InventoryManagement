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

/// <summary>Payload for returning an assignment.</summary>
public class ReturnInventoryAssignmentDto
{
    /// <summary>Assignment to return (required).</summary>
    [Range(1, int.MaxValue)]
    public int AssignmentId { get; set; }

    /// <summary>Optional return notes (≤1000 chars).</summary>
    [StringLength(1000)]
    public string? ReturnNotes { get; set; }
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
