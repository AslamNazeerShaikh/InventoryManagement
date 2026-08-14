using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;

namespace InventoryManagement.Domain.Interfaces;

/// <summary>Repository for <see cref="InventoryAssignment"/> aggregates with allocation-oriented queries.</summary>
public interface IInventoryAssignmentRepository : IGenericRepository<InventoryAssignment>
{
    /// <summary>Lists a user's assignments (with related graph), newest first.</summary>
    Task<IReadOnlyList<InventoryAssignment>> GetAssignmentsByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists assignments for an inventory item (with related graph), newest first.</summary>
    Task<IReadOnlyList<InventoryAssignment>> GetAssignmentsByInventoryIdAsync(
        int inventoryId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists all active assignments (with related graph), newest first.</summary>
    Task<IReadOnlyList<InventoryAssignment>> GetActiveAssignmentsAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists a user's active assignments (with related graph), newest first.</summary>
    Task<IReadOnlyList<InventoryAssignment>> GetActiveAssignmentsByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists assignments in a given status (with related graph), newest first.</summary>
    Task<IReadOnlyList<InventoryAssignment>> GetAssignmentsByStatusAsync(
        AssignmentStatus status,
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists active assignments past their expected return date (with related graph).</summary>
    Task<IReadOnlyList<InventoryAssignment>> GetOverdueAssignmentsAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>Finds the active assignment for an inventory/user pair, or <c>null</c>.</summary>
    Task<InventoryAssignment?> GetActiveAssignmentAsync(
        int inventoryId,
        int userId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Returns whether an active assignment exists for an inventory/user pair.</summary>
    Task<bool> HasActiveAssignmentAsync(
        int inventoryId,
        int userId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists the full assignment history for an inventory item (with related graph), newest first.</summary>
    Task<IReadOnlyList<InventoryAssignment>> GetAssignmentHistoryAsync(
        int inventoryId,
        CancellationToken cancellationToken = default
    );
}
