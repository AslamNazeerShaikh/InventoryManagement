using InventoryManagement.Application.Mapping;
using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Application.Services;

/// <summary>
/// Assignment service. Stock-changing operations run inside a transaction and rely on the entity
/// concurrency token to prevent oversell under concurrent requests; conflicts surface as HTTP 409.
/// </summary>
public sealed class InventoryAssignmentService : IInventoryAssignmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InventoryAssignmentService> _logger;

    /// <summary>Creates the assignment service.</summary>
    public InventoryAssignmentService(
        IUnitOfWork unitOfWork,
        ILogger<InventoryAssignmentService> logger
    )
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>> GetAllAssignmentsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var assignments = await _unitOfWork
            .InventoryAssignments.ListAsync(
                include: q =>
                    q.Include(x => x.Inventory)
                        .Include(x => x.User)
                        .Include(x => x.AssignedByUser),
                orderBy: q => q.OrderByDescending(x => x.AssignedDate),
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);
        return ApiResponse<IEnumerable<InventoryAssignmentDto>>.Success(assignments.ToDto());
    }

    /// <inheritdoc />
    public async Task<ApiResponse<InventoryAssignmentDto>> GetAssignmentByIdAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        var assignment = await _unitOfWork
            .InventoryAssignments.FirstOrDefaultAsync(
                x => x.Id == id,
                include: q =>
                    q.Include(a => a.Inventory)
                        .Include(a => a.User)
                        .Include(a => a.AssignedByUser)
                        .Include(a => a.ReturnedToUser),
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        return assignment is null
            ? ApiResponse<InventoryAssignmentDto>.Failure("Assignment not found")
            : ApiResponse<InventoryAssignmentDto>.Success(assignment.ToDto());
    }

    /// <inheritdoc />
    public async Task<ApiResponse<InventoryAssignmentDto>> CreateAssignmentAsync(
        CreateInventoryAssignmentDto createAssignmentDto,
        int assignedByUserId,
        CancellationToken cancellationToken = default
    )
    {
        var outcome = await _unitOfWork
            .ExecuteInTransactionAsync(
                async ct =>
                {
                    var inventory = await _unitOfWork.Inventories.GetByIdAsync(
                        createAssignmentDto.InventoryId,
                        ct
                    )
                        .ConfigureAwait(false);
                    if (inventory is null)
                    {
                        return Failed("Inventory not found");
                    }

                    if (inventory.Status != InventoryStatus.Available)
                    {
                        return Failed("Inventory is not available for assignment");
                    }

                    if (inventory.ExpiryDate.HasValue && inventory.ExpiryDate.Value < DateTime.UtcNow)
                    {
                        return Failed("Cannot assign expired inventory");
                    }

                    if (inventory.AvailableQuantity < createAssignmentDto.AssignedQuantity)
                    {
                        return Failed(
                            $"Insufficient quantity. Available: {inventory.AvailableQuantity}, "
                                + $"Requested: {createAssignmentDto.AssignedQuantity}"
                        );
                    }

                    var user = await _unitOfWork.Users.GetByIdAsync(createAssignmentDto.UserId, ct)
                        .ConfigureAwait(false);
                    if (user is null)
                    {
                        return Failed("User not found");
                    }

                    var assignment = createAssignmentDto.ToEntity();
                    assignment.AssignedByUserId = assignedByUserId;
                    assignment.Status = AssignmentStatus.Active;
                    await _unitOfWork.InventoryAssignments.AddAsync(assignment, ct)
                        .ConfigureAwait(false);

                    inventory.AvailableQuantity -= createAssignmentDto.AssignedQuantity;
                    if (inventory.AvailableQuantity == 0)
                    {
                        inventory.Status = InventoryStatus.Assigned;
                    }

                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                    return Succeeded(assignment.Id);
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        if (!outcome.Success)
        {
            return ApiResponse<InventoryAssignmentDto>.Failure(outcome.Error!);
        }

        _logger.LogInformation("Created assignment {AssignmentId}.", outcome.AssignmentId);
        var created = await LoadForDtoAsync(outcome.AssignmentId, cancellationToken)
            .ConfigureAwait(false);
        return ApiResponse<InventoryAssignmentDto>.Success(
            created!.ToDto(),
            "Assignment created successfully"
        );
    }

    /// <inheritdoc />
    public async Task<ApiResponse<InventoryAssignmentDto>> UpdateAssignmentAsync(
        int id,
        UpdateInventoryAssignmentDto updateAssignmentDto,
        CancellationToken cancellationToken = default
    )
    {
        var outcome = await _unitOfWork
            .ExecuteInTransactionAsync(
                async ct =>
                {
                    var assignment = await _unitOfWork
                        .InventoryAssignments.FirstOrDefaultAsync(
                            x => x.Id == id,
                            include: q => q.Include(a => a.Inventory),
                            asNoTracking: false,
                            cancellationToken: ct
                        )
                        .ConfigureAwait(false);

                    if (assignment is null)
                    {
                        return Failed("Assignment not found");
                    }

                    if (assignment.Status != AssignmentStatus.Active)
                    {
                        return Failed("Can only update active assignments");
                    }

                    if (updateAssignmentDto.AssignedQuantity != assignment.AssignedQuantity)
                    {
                        var difference =
                            updateAssignmentDto.AssignedQuantity - assignment.AssignedQuantity;

                        if (difference > 0)
                        {
                            if (assignment.Inventory.AvailableQuantity < difference)
                            {
                                return Failed("Insufficient inventory quantity available");
                            }

                            assignment.Inventory.AvailableQuantity -= difference;
                            if (assignment.Inventory.AvailableQuantity == 0)
                            {
                                assignment.Inventory.Status = InventoryStatus.Assigned;
                            }
                        }
                        else
                        {
                            assignment.Inventory.AvailableQuantity += Math.Abs(difference);
                            if (
                                assignment.Inventory.Status == InventoryStatus.Assigned
                                && assignment.Inventory.AvailableQuantity > 0
                            )
                            {
                                assignment.Inventory.Status = InventoryStatus.Available;
                            }
                        }
                    }

                    updateAssignmentDto.UpdateEntity(assignment);
                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                    return Succeeded(assignment.Id);
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        if (!outcome.Success)
        {
            return ApiResponse<InventoryAssignmentDto>.Failure(outcome.Error!);
        }

        _logger.LogInformation("Updated assignment {AssignmentId}.", id);
        var updated = await LoadForDtoAsync(id, cancellationToken).ConfigureAwait(false);
        return ApiResponse<InventoryAssignmentDto>.Success(
            updated!.ToDto(),
            "Assignment updated successfully"
        );
    }

    /// <inheritdoc />
    public async Task<ApiResponse<bool>> ReturnAssignmentAsync(
        ReturnInventoryAssignmentDto returnAssignmentDto,
        int returnedToUserId,
        CancellationToken cancellationToken = default
    )
    {
        var outcome = await _unitOfWork
            .ExecuteInTransactionAsync(
                async ct =>
                {
                    var assignment = await _unitOfWork
                        .InventoryAssignments.FirstOrDefaultAsync(
                            x => x.Id == returnAssignmentDto.AssignmentId,
                            include: q => q.Include(a => a.Inventory),
                            asNoTracking: false,
                            cancellationToken: ct
                        )
                        .ConfigureAwait(false);

                    if (assignment is null)
                    {
                        return Failed("Assignment not found");
                    }

                    if (assignment.Status != AssignmentStatus.Active)
                    {
                        return Failed("Assignment is not active");
                    }

                    assignment.Status = AssignmentStatus.Returned;
                    assignment.ReturnDate = DateTime.UtcNow;
                    assignment.ReturnedToUserId = returnedToUserId;
                    assignment.ReturnNotes = returnAssignmentDto.ReturnNotes;

                    var inventory = assignment.Inventory;
                    // Restore stock without exceeding the total owned quantity (invariant guard).
                    inventory.AvailableQuantity = Math.Min(
                        inventory.Quantity,
                        inventory.AvailableQuantity + assignment.AssignedQuantity
                    );

                    // Only a fully-assigned item returns to Available; never override
                    // Damaged/Expired/Disposed lifecycle states.
                    if (
                        inventory.Status == InventoryStatus.Assigned
                        && inventory.AvailableQuantity > 0
                    )
                    {
                        inventory.Status = InventoryStatus.Available;
                    }

                    await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                    return Succeeded(assignment.Id);
                },
                cancellationToken
            )
            .ConfigureAwait(false);

        if (!outcome.Success)
        {
            return ApiResponse<bool>.Failure(outcome.Error!);
        }

        _logger.LogInformation(
            "Returned assignment {AssignmentId}.",
            returnAssignmentDto.AssignmentId
        );
        return ApiResponse<bool>.Success(true, "Assignment returned successfully");
    }

    /// <inheritdoc />
    public async Task<
        ApiResponse<IEnumerable<InventoryAssignmentDto>>
    > GetAssignmentsByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var assignments = await _unitOfWork
            .InventoryAssignments.GetAssignmentsByUserIdAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        return ApiResponse<IEnumerable<InventoryAssignmentDto>>.Success(assignments.ToDto());
    }

    /// <inheritdoc />
    public async Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>> GetActiveAssignmentsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var assignments = await _unitOfWork.InventoryAssignments.GetActiveAssignmentsAsync(
            cancellationToken
        )
            .ConfigureAwait(false);
        return ApiResponse<IEnumerable<InventoryAssignmentDto>>.Success(assignments.ToDto());
    }

    /// <inheritdoc />
    public async Task<
        ApiResponse<IEnumerable<InventoryAssignmentDto>>
    > GetActiveAssignmentsByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var assignments = await _unitOfWork
            .InventoryAssignments.GetActiveAssignmentsByUserIdAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        return ApiResponse<IEnumerable<InventoryAssignmentDto>>.Success(assignments.ToDto());
    }

    /// <inheritdoc />
    public async Task<ApiResponse<IEnumerable<InventoryAssignmentDto>>> GetOverdueAssignmentsAsync(
        CancellationToken cancellationToken = default
    )
    {
        var assignments = await _unitOfWork.InventoryAssignments.GetOverdueAssignmentsAsync(
            cancellationToken
        )
            .ConfigureAwait(false);
        return ApiResponse<IEnumerable<InventoryAssignmentDto>>.Success(assignments.ToDto());
    }

    /// <inheritdoc />
    public async Task<ApiResponse<AssignmentHistoryDto>> GetAssignmentHistoryAsync(
        int inventoryId,
        CancellationToken cancellationToken = default
    )
    {
        var inventory = await _unitOfWork.Inventories.GetByIdAsync(inventoryId, cancellationToken)
            .ConfigureAwait(false);
        if (inventory is null)
        {
            return ApiResponse<AssignmentHistoryDto>.Failure("Inventory not found");
        }

        var assignments = await _unitOfWork
            .InventoryAssignments.GetAssignmentHistoryAsync(inventoryId, cancellationToken)
            .ConfigureAwait(false);

        var historyDto = new AssignmentHistoryDto
        {
            InventoryId = inventoryId,
            EquipmentName = inventory.EquipmentName,
            Assignments = assignments.ToDto().ToList(),
        };

        return ApiResponse<AssignmentHistoryDto>.Success(historyDto);
    }

    /// <inheritdoc />
    public async Task<ApiResponse<PagedResult<InventoryAssignmentDto>>> GetAssignmentsPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default
    )
    {
        pageSize = Math.Clamp(pageSize, 1, BusinessConstants.Pagination.MaxPageSize);

        // Include the related graph so DTO projection has all navigation data (fixes prior NRE).
        var page = await _unitOfWork
            .InventoryAssignments.GetPagedAsync(
                pageNumber,
                pageSize,
                orderBy: q => q.OrderByDescending(x => x.AssignedDate),
                include: q =>
                    q.Include(x => x.Inventory)
                        .Include(x => x.User)
                        .Include(x => x.AssignedByUser),
                cancellationToken: cancellationToken
            )
            .ConfigureAwait(false);

        var result = new PagedResult<InventoryAssignmentDto>
        {
            Data = page.Items.ToDto(),
            TotalCount = page.TotalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
        };

        return ApiResponse<PagedResult<InventoryAssignmentDto>>.Success(result);
    }

    /// <summary>Loads a single assignment (tracking-free) with its related graph for projection.</summary>
    private Task<InventoryAssignment?> LoadForDtoAsync(
        int id,
        CancellationToken cancellationToken
    ) =>
        _unitOfWork.InventoryAssignments.FirstOrDefaultAsync(
            x => x.Id == id,
            include: q =>
                q.Include(a => a.Inventory).Include(a => a.User).Include(a => a.AssignedByUser),
            cancellationToken: cancellationToken
        );

    // Small transactional result helpers keep the delegate bodies readable and allocation-light.
    private static TransactionOutcome Failed(string error) => new(false, 0, error);

    private static TransactionOutcome Succeeded(int assignmentId) => new(true, assignmentId, null);

    private readonly record struct TransactionOutcome(bool Success, int AssignmentId, string? Error);
}
