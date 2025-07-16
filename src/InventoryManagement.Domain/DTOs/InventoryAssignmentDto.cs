using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.DTOs;

public class InventoryAssignmentDto
{
    public int Id { get; set; }
    public int InventoryId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Barcode { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public int AssignedQuantity { get; set; }
    public DateTime AssignedDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public AssignmentStatus Status { get; set; }
    public string? AssignmentNotes { get; set; }
    public string? ReturnNotes { get; set; }
    public string? AssignedByUserName { get; set; }
    public string? ReturnedToUserName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateInventoryAssignmentDto
{
    public int InventoryId { get; set; }
    public int UserId { get; set; }
    public int AssignedQuantity { get; set; } = 1;
    public DateTime? ExpectedReturnDate { get; set; }
    public string? AssignmentNotes { get; set; }
}

public class ReturnInventoryAssignmentDto
{
    public int AssignmentId { get; set; }
    public string? ReturnNotes { get; set; }
}

public class UpdateInventoryAssignmentDto
{
    public int AssignedQuantity { get; set; }
    public DateTime? ExpectedReturnDate { get; set; }
    public AssignmentStatus Status { get; set; }
    public string? AssignmentNotes { get; set; }
}

public class AssignmentHistoryDto
{
    public int InventoryId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public List<InventoryAssignmentDto> Assignments { get; set; } = new();
}
