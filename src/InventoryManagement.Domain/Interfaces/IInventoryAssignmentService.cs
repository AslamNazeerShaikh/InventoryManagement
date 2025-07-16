using InventoryManagement.Domain.DTOs;

namespace InventoryManagement.Domain.Interfaces;

public interface IInventoryAssignmentService
{
    Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>> GetAllAssignmentsAsync();
    Task<ApiResponse<InventoryAssignmentDto>> GetAssignmentByIdAsync(int id);
    Task<ApiResponse<InventoryAssignmentDto>> CreateAssignmentAsync(
        CreateInventoryAssignmentDto createAssignmentDto,
        int assignedByUserId
    );
    Task<ApiResponse<InventoryAssignmentDto>> UpdateAssignmentAsync(
        int id,
        UpdateInventoryAssignmentDto updateAssignmentDto
    );
    Task<ApiResponse<bool>> ReturnAssignmentAsync(
        ReturnInventoryAssignmentDto returnAssignmentDto,
        int returnedToUserId
    );
    Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>> GetAssignmentsByUserIdAsync(int userId);
    Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>> GetActiveAssignmentsAsync();
    Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>> GetActiveAssignmentsByUserIdAsync(
        int userId
    );
    Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>> GetOverdueAssignmentsAsync();
    Task<ApiResponse<AssignmentHistoryDto>> GetAssignmentHistoryAsync(int inventoryId);
    Task<ApiResponse<PagedResult<InventoryAssignmentDto>>> GetAssignmentsPagedAsync(
        int pageNumber,
        int pageSize
    );
}
