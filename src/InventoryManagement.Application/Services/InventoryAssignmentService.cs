using InventoryManagement.Application.Mapping;
using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Domain.Interfaces;

namespace InventoryManagement.Application.Services;

public class InventoryAssignmentService : IInventoryAssignmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public InventoryAssignmentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>> GetAllAssignmentsAsync()
    {
        try
        {
            var assignments = await _unitOfWork.InventoryAssignments.GetAllAsync(
                x => x.Inventory,
                x => x.User,
                x => x.AssignedByUser
            );
            var assignmentDtos = assignments.ToDto();
            return ApiResponse<IEnumerable<InventoryAssignmentDto>>.Success(assignmentDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<InventoryAssignmentDto>>.Failure(
                $"Error retrieving assignments: {ex.Message}"
            );
        }
    }

    public async Task<ApiResponse<InventoryAssignmentDto>> GetAssignmentByIdAsync(int id)
    {
        try
        {
            var assignment = await _unitOfWork.InventoryAssignments.GetByIdAsync(
                id,
                x => x.Inventory,
                x => x.User,
                x => x.AssignedByUser,
                x => x.ReturnedToUser
            );

            if (assignment == null)
            {
                return ApiResponse<InventoryAssignmentDto>.Failure("Assignment not found");
            }

            return ApiResponse<InventoryAssignmentDto>.Success(assignment.ToDto());
        }
        catch (Exception ex)
        {
            return ApiResponse<InventoryAssignmentDto>.Failure(
                $"Error retrieving assignment: {ex.Message}"
            );
        }
    }

    public async Task<ApiResponse<InventoryAssignmentDto>> CreateAssignmentAsync(
        CreateInventoryAssignmentDto createAssignmentDto,
        int assignedByUserId
    )
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            // Validate inventory exists and is available
            var inventory = await _unitOfWork.Inventories.GetByIdAsync(
                createAssignmentDto.InventoryId
            );
            if (inventory == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ApiResponse<InventoryAssignmentDto>.Failure("Inventory not found");
            }

            if (inventory.Status != InventoryStatus.Available)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ApiResponse<InventoryAssignmentDto>.Failure(
                    "Inventory is not available for assignment"
                );
            }

            // Check if inventory is expired
            if (inventory.ExpiryDate.HasValue && inventory.ExpiryDate.Value < DateTime.UtcNow)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ApiResponse<InventoryAssignmentDto>.Failure(
                    "Cannot assign expired inventory"
                );
            }

            // Check available quantity
            if (inventory.AvailableQuantity < createAssignmentDto.AssignedQuantity)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ApiResponse<InventoryAssignmentDto>.Failure(
                    $"Insufficient quantity. Available: {inventory.AvailableQuantity}, Requested: {createAssignmentDto.AssignedQuantity}"
                );
            }

            // Validate user exists
            var user = await _unitOfWork.Users.GetByIdAsync(createAssignmentDto.UserId);
            if (user == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ApiResponse<InventoryAssignmentDto>.Failure("User not found");
            }

            // Create assignment
            var assignment = createAssignmentDto.ToEntity();
            assignment.AssignedByUserId = assignedByUserId;
            assignment.Status = AssignmentStatus.Active;

            await _unitOfWork.InventoryAssignments.AddAsync(assignment);

            // Update inventory available quantity
            inventory.AvailableQuantity -= createAssignmentDto.AssignedQuantity;
            if (inventory.AvailableQuantity == 0)
            {
                inventory.Status = InventoryStatus.Assigned;
            }
            _unitOfWork.Inventories.Update(inventory);

            await _unitOfWork.SaveAsync();
            await _unitOfWork.CommitTransactionAsync();

            // Get the created assignment with full details
            var createdAssignment = await _unitOfWork.InventoryAssignments.GetByIdAsync(
                assignment.Id,
                x => x.Inventory,
                x => x.User,
                x => x.AssignedByUser
            );

            return ApiResponse<InventoryAssignmentDto>.Success(
                createdAssignment!.ToDto(),
                "Assignment created successfully"
            );
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return ApiResponse<InventoryAssignmentDto>.Failure(
                $"Error creating assignment: {ex.Message}"
            );
        }
    }

    public async Task<ApiResponse<InventoryAssignmentDto>> UpdateAssignmentAsync(
        int id,
        UpdateInventoryAssignmentDto updateAssignmentDto
    )
    {
        try
        {
            var assignment = await _unitOfWork.InventoryAssignments.GetByIdAsync(
                id,
                x => x.Inventory
            );
            if (assignment == null)
            {
                return ApiResponse<InventoryAssignmentDto>.Failure("Assignment not found");
            }

            if (assignment.Status != AssignmentStatus.Active)
            {
                return ApiResponse<InventoryAssignmentDto>.Failure(
                    "Can only update active assignments"
                );
            }

            // If quantity is being changed, validate inventory availability
            if (updateAssignmentDto.AssignedQuantity != assignment.AssignedQuantity)
            {
                var quantityDifference =
                    updateAssignmentDto.AssignedQuantity - assignment.AssignedQuantity;

                if (quantityDifference > 0) // Increasing quantity
                {
                    if (assignment.Inventory.AvailableQuantity < quantityDifference)
                    {
                        return ApiResponse<InventoryAssignmentDto>.Failure(
                            "Insufficient inventory quantity available"
                        );
                    }

                    assignment.Inventory.AvailableQuantity -= quantityDifference;
                }
                else // Decreasing quantity
                {
                    assignment.Inventory.AvailableQuantity += Math.Abs(quantityDifference);

                    // Update inventory status if it becomes available again
                    if (
                        assignment.Inventory.Status == InventoryStatus.Assigned
                        && assignment.Inventory.AvailableQuantity > 0
                    )
                    {
                        assignment.Inventory.Status = InventoryStatus.Available;
                    }
                }

                _unitOfWork.Inventories.Update(assignment.Inventory);
            }

            // Update assignment
            updateAssignmentDto.UpdateEntity(assignment);
            _unitOfWork.InventoryAssignments.Update(assignment);
            await _unitOfWork.SaveAsync();

            // Get updated assignment with full details
            var updatedAssignment = await _unitOfWork.InventoryAssignments.GetByIdAsync(
                id,
                x => x.Inventory,
                x => x.User,
                x => x.AssignedByUser
            );

            return ApiResponse<InventoryAssignmentDto>.Success(
                updatedAssignment!.ToDto(),
                "Assignment updated successfully"
            );
        }
        catch (Exception ex)
        {
            return ApiResponse<InventoryAssignmentDto>.Failure(
                $"Error updating assignment: {ex.Message}"
            );
        }
    }

    public async Task<ApiResponse<bool>> ReturnAssignmentAsync(
        ReturnInventoryAssignmentDto returnAssignmentDto,
        int returnedToUserId
    )
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var assignment = await _unitOfWork.InventoryAssignments.GetByIdAsync(
                returnAssignmentDto.AssignmentId,
                x => x.Inventory
            );
            if (assignment == null)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ApiResponse<bool>.Failure("Assignment not found");
            }

            if (assignment.Status != AssignmentStatus.Active)
            {
                await _unitOfWork.RollbackTransactionAsync();
                return ApiResponse<bool>.Failure("Assignment is not active");
            }

            // Update assignment status
            assignment.Status = AssignmentStatus.Returned;
            assignment.ReturnDate = DateTime.UtcNow;
            assignment.ReturnedToUserId = returnedToUserId;
            assignment.ReturnNotes = returnAssignmentDto.ReturnNotes;

            // Return quantity to inventory
            assignment.Inventory.AvailableQuantity += assignment.AssignedQuantity;
            assignment.Inventory.Status = InventoryStatus.Available;

            _unitOfWork.InventoryAssignments.Update(assignment);
            _unitOfWork.Inventories.Update(assignment.Inventory);

            await _unitOfWork.SaveAsync();
            await _unitOfWork.CommitTransactionAsync();

            return ApiResponse<bool>.Success(true, "Assignment returned successfully");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return ApiResponse<bool>.Failure($"Error returning assignment: {ex.Message}");
        }
    }

    public async Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>> GetAssignmentsByUserIdAsync(
        int userId
    )
    {
        try
        {
            var assignments = await _unitOfWork.InventoryAssignments.GetAssignmentsByUserIdAsync(
                userId
            );
            var assignmentDtos = assignments.ToDto();
            return ApiResponse<IEnumerable<InventoryAssignmentDto>>.Success(assignmentDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<InventoryAssignmentDto>>.Failure(
                $"Error retrieving user assignments: {ex.Message}"
            );
        }
    }

    public async Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>> GetActiveAssignmentsAsync()
    {
        try
        {
            var assignments = await _unitOfWork.InventoryAssignments.GetActiveAssignmentsAsync();
            var assignmentDtos = assignments.ToDto();
            return ApiResponse<IEnumerable<InventoryAssignmentDto>>.Success(assignmentDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<InventoryAssignmentDto>>.Failure(
                $"Error retrieving active assignments: {ex.Message}"
            );
        }
    }

    public async Task<
        ApiResponse<IEnumerable<InventoryAssignmentDto>>
    > GetActiveAssignmentsByUserIdAsync(int userId)
    {
        try
        {
            var assignments =
                await _unitOfWork.InventoryAssignments.GetActiveAssignmentsByUserIdAsync(userId);
            var assignmentDtos = assignments.ToDto();
            return ApiResponse<IEnumerable<InventoryAssignmentDto>>.Success(assignmentDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<InventoryAssignmentDto>>.Failure(
                $"Error retrieving user active assignments: {ex.Message}"
            );
        }
    }

    public async Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>> GetOverdueAssignmentsAsync()
    {
        try
        {
            var assignments = await _unitOfWork.InventoryAssignments.GetOverdueAssignmentsAsync();
            var assignmentDtos = assignments.ToDto();
            return ApiResponse<IEnumerable<InventoryAssignmentDto>>.Success(assignmentDtos);
        }
        catch (Exception ex)
        {
            return ApiResponse<IEnumerable<InventoryAssignmentDto>>.Failure(
                $"Error retrieving overdue assignments: {ex.Message}"
            );
        }
    }

    public async Task<ApiResponse<AssignmentHistoryDto>> GetAssignmentHistoryAsync(int inventoryId)
    {
        try
        {
            var inventory = await _unitOfWork.Inventories.GetByIdAsync(inventoryId);
            if (inventory == null)
            {
                return ApiResponse<AssignmentHistoryDto>.Failure("Inventory not found");
            }

            var assignments = await _unitOfWork.InventoryAssignments.GetAssignmentHistoryAsync(
                inventoryId
            );

            var historyDto = new AssignmentHistoryDto
            {
                InventoryId = inventoryId,
                EquipmentName = inventory.EquipmentName,
                Assignments = assignments.ToDto().ToList(),
            };

            return ApiResponse<AssignmentHistoryDto>.Success(historyDto);
        }
        catch (Exception ex)
        {
            return ApiResponse<AssignmentHistoryDto>.Failure(
                $"Error retrieving assignment history: {ex.Message}"
            );
        }
    }

    public async Task<ApiResponse<PagedResult<InventoryAssignmentDto>>> GetAssignmentsPagedAsync(
        int pageNumber,
        int pageSize
    )
    {
        try
        {
            pageSize = Math.Min(pageSize, BusinessConstants.Pagination.MaxPageSize);

            var assignments = await _unitOfWork.InventoryAssignments.GetPagedAsync(
                pageNumber,
                pageSize
            );
            var totalCount = await _unitOfWork.InventoryAssignments.CountAsync();

            var pagedResult = new PagedResult<InventoryAssignmentDto>
            {
                Data = assignments.ToDto(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
            };

            return ApiResponse<PagedResult<InventoryAssignmentDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            return ApiResponse<PagedResult<InventoryAssignmentDto>>.Failure(
                $"Error retrieving paged assignments: {ex.Message}"
            );
        }
    }
}
