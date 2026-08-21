using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.DTOs;

namespace InventoryManagement.Domain.Interfaces;

/// <summary>Assignment (allocation/return) operations for inventory items.</summary>
public interface IInventoryAssignmentService
{
    /// <summary>Lists all assignments.</summary>
    Task<Result<IEnumerable<InventoryAssignmentDto>>> GetAllAssignmentsAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>Gets an assignment by identifier.</summary>
    Task<Result<InventoryAssignmentDto>> GetAssignmentByIdAsync(
        int id,
        CancellationToken cancellationToken = default
    );

    /// <summary>Creates an assignment, decrementing available stock atomically.</summary>
    Task<Result<InventoryAssignmentDto>> CreateAssignmentAsync(
        CreateInventoryAssignmentDto createAssignmentDto,
        int assignedByUserId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Updates an active assignment, adjusting stock consistently.</summary>
    Task<Result<InventoryAssignmentDto>> UpdateAssignmentAsync(
        int id,
        UpdateInventoryAssignmentDto updateAssignmentDto,
        CancellationToken cancellationToken = default
    );

    /// <summary>Processes a return (full or partial), restoring stock atomically.</summary>
    Task<Result<bool>> ReturnAssignmentAsync(
        ReturnInventoryAssignmentDto returnAssignmentDto,
        int returnedToUserId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Renews/extends an active assignment's expected return date.</summary>
    Task<Result<InventoryAssignmentDto>> RenewAssignmentAsync(
        RenewInventoryAssignmentDto renewAssignmentDto,
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists a user's assignments.</summary>
    Task<Result<IEnumerable<InventoryAssignmentDto>>> GetAssignmentsByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists active assignments.</summary>
    Task<Result<IEnumerable<InventoryAssignmentDto>>> GetActiveAssignmentsAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists a user's active assignments.</summary>
    Task<Result<IEnumerable<InventoryAssignmentDto>>> GetActiveAssignmentsByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists overdue assignments.</summary>
    Task<Result<IEnumerable<InventoryAssignmentDto>>> GetOverdueAssignmentsAsync(
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists active assignments due within the given number of days (upcoming return reminders).</summary>
    Task<Result<IEnumerable<InventoryAssignmentDto>>> GetDueSoonAssignmentsAsync(
        int daysAhead = 7,
        CancellationToken cancellationToken = default
    );

    /// <summary>Returns the assignment history for an inventory item.</summary>
    Task<Result<AssignmentHistoryDto>> GetAssignmentHistoryAsync(
        int inventoryId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Returns a deterministic page of assignments.</summary>
    Task<Result<PagedResult<InventoryAssignmentDto>>> GetAssignmentsPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default
    );
}
