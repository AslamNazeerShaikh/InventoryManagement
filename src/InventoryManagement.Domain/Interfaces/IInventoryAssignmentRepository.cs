using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.Interfaces;

public interface IInventoryAssignmentRepository : IGenericRepository<InventoryAssignment>
{
    Task<IEnumerable<InventoryAssignment>> GetAssignmentsByUserIdAsync(int userId);
    Task<IEnumerable<InventoryAssignment>> GetAssignmentsByInventoryIdAsync(int inventoryId);
    Task<IEnumerable<InventoryAssignment>> GetActiveAssignmentsAsync();
    Task<IEnumerable<InventoryAssignment>> GetActiveAssignmentsByUserIdAsync(int userId);
    Task<IEnumerable<InventoryAssignment>> GetAssignmentsByStatusAsync(AssignmentStatus status);
    Task<IEnumerable<InventoryAssignment>> GetOverdueAssignmentsAsync();
    Task<InventoryAssignment?> GetActiveAssignmentAsync(int inventoryId, int userId);
    Task<bool> HasActiveAssignmentAsync(int inventoryId, int userId);
    Task ReturnAssignmentAsync(int assignmentId, int returnedToUserId, string? returnNotes = null);
    Task<IEnumerable<InventoryAssignment>> GetAssignmentHistoryAsync(int inventoryId);
}
