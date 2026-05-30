using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = AuthConstants.Policies.AllRoles)]
public class InventoryAssignmentsController : ControllerBase
{
    private readonly IInventoryAssignmentService _assignmentService;
    private readonly ILogger<InventoryAssignmentsController> _logger;

    public InventoryAssignmentsController(
        IInventoryAssignmentService assignmentService,
        ILogger<InventoryAssignmentsController> logger
    )
    {
        _assignmentService = assignmentService;
        _logger = logger;
    }

    /// <summary>
    /// Get all assignments (Admin or Provider)
    /// </summary>
    /// <returns>List of all assignments</returns>
    [HttpGet]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<InventoryAssignmentDto>>>
    > GetAllAssignments()
    {
        try
        {
            _logger.LogInformation("Requesting all assignments");
            var result = await _assignmentService.GetAllAssignmentsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all assignments");
            return StatusCode(
                500,
                ApiResponse<IEnumerable<InventoryAssignmentDto>>.Failure(
                    "An error occurred while retrieving assignments"
                )
            );
        }
    }

    /// <summary>
    /// Get assignments with pagination (Admin or Provider)
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <returns>Paginated list of assignments</returns>
    [HttpGet("paged")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<
        ActionResult<ApiResponse<PagedResult<InventoryAssignmentDto>>>
    > GetAssignmentsPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            if (pageSize > BusinessConstants.Pagination.MaxPageSize)
                pageSize = BusinessConstants.Pagination.MaxPageSize;

            _logger.LogInformation(
                "Requesting assignments page {PageNumber} with size {PageSize}",
                pageNumber,
                pageSize
            );
            var result = await _assignmentService.GetAssignmentsPagedAsync(pageNumber, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paged assignments");
            return StatusCode(
                500,
                ApiResponse<PagedResult<InventoryAssignmentDto>>.Failure(
                    "An error occurred while retrieving assignments"
                )
            );
        }
    }

    /// <summary>
    /// Get assignment by ID
    /// </summary>
    /// <param name="id">Assignment ID</param>
    /// <returns>Assignment details</returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<InventoryAssignmentDto>>> GetAssignmentById(int id)
    {
        try
        {
            var currentUserIdClaim = User.FindFirst(AuthConstants.Claims.UserId)?.Value;
            var isAdmin = User.FindFirst(AuthConstants.Claims.IsAdmin)?.Value == "True";
            var isProvider = User.FindFirst(AuthConstants.Claims.IsProvider)?.Value == "True";

            if (!int.TryParse(currentUserIdClaim, out int currentUserId))
            {
                return BadRequest(
                    ApiResponse<InventoryAssignmentDto>.Failure("Invalid user ID in token")
                );
            }

            _logger.LogInformation(
                "User {UserId} requesting assignment {AssignmentId}",
                currentUserId,
                id
            );
            var result = await _assignmentService.GetAssignmentByIdAsync(id);

            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            // Allow access if user is admin/provider or it's their own assignment
            if (!isAdmin && !isProvider && result.Data!.UserId != currentUserId)
            {
                return Forbid();
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assignment {AssignmentId}", id);
            return StatusCode(
                500,
                ApiResponse<InventoryAssignmentDto>.Failure(
                    "An error occurred while retrieving assignment"
                )
            );
        }
    }

    /// <summary>
    /// Get assignments by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of user's assignments</returns>
    [HttpGet("user/{userId:int}")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<InventoryAssignmentDto>>>
    > GetAssignmentsByUserId(int userId)
    {
        try
        {
            var currentUserIdClaim = User.FindFirst(AuthConstants.Claims.UserId)?.Value;
            var isAdmin = User.FindFirst(AuthConstants.Claims.IsAdmin)?.Value == "True";
            var isProvider = User.FindFirst(AuthConstants.Claims.IsProvider)?.Value == "True";

            if (!int.TryParse(currentUserIdClaim, out int currentUserId))
            {
                return BadRequest(
                    ApiResponse<IEnumerable<InventoryAssignmentDto>>.Failure(
                        "Invalid user ID in token"
                    )
                );
            }

            // Allow access if user is admin/provider or requesting their own assignments
            if (!isAdmin && !isProvider && currentUserId != userId)
            {
                return Forbid();
            }

            _logger.LogInformation("Requesting assignments for user {UserId}", userId);
            var result = await _assignmentService.GetAssignmentsByUserIdAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assignments for user {UserId}", userId);
            return StatusCode(
                500,
                ApiResponse<IEnumerable<InventoryAssignmentDto>>.Failure(
                    "An error occurred while retrieving user assignments"
                )
            );
        }
    }

    /// <summary>
    /// Get current user's assignments
    /// </summary>
    /// <returns>List of current user's assignments</returns>
    [HttpGet("my-assignments")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<InventoryAssignmentDto>>>
    > GetMyAssignments()
    {
        try
        {
            var currentUserIdClaim = User.FindFirst(AuthConstants.Claims.UserId)?.Value;
            if (!int.TryParse(currentUserIdClaim, out int currentUserId))
            {
                return BadRequest(
                    ApiResponse<IEnumerable<InventoryAssignmentDto>>.Failure(
                        "Invalid user ID in token"
                    )
                );
            }

            _logger.LogInformation("User {UserId} requesting their assignments", currentUserId);
            var result = await _assignmentService.GetAssignmentsByUserIdAsync(currentUserId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user assignments");
            return StatusCode(
                500,
                ApiResponse<IEnumerable<InventoryAssignmentDto>>.Failure(
                    "An error occurred while retrieving your assignments"
                )
            );
        }
    }

    /// <summary>
    /// Get active assignments (Admin or Provider)
    /// </summary>
    /// <returns>List of active assignments</returns>
    [HttpGet("active")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<InventoryAssignmentDto>>>
    > GetActiveAssignments()
    {
        try
        {
            _logger.LogInformation("Requesting active assignments");
            var result = await _assignmentService.GetActiveAssignmentsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active assignments");
            return StatusCode(
                500,
                ApiResponse<IEnumerable<InventoryAssignmentDto>>.Failure(
                    "An error occurred while retrieving active assignments"
                )
            );
        }
    }

    /// <summary>
    /// Get active assignments for a specific user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>List of active assignments for the user</returns>
    [HttpGet("active/user/{userId:int}")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<InventoryAssignmentDto>>>
    > GetActiveAssignmentsByUserId(int userId)
    {
        try
        {
            var currentUserIdClaim = User.FindFirst(AuthConstants.Claims.UserId)?.Value;
            var isAdmin = User.FindFirst(AuthConstants.Claims.IsAdmin)?.Value == "True";
            var isProvider = User.FindFirst(AuthConstants.Claims.IsProvider)?.Value == "True";

            if (!int.TryParse(currentUserIdClaim, out int currentUserId))
            {
                return BadRequest(
                    ApiResponse<IEnumerable<InventoryAssignmentDto>>.Failure(
                        "Invalid user ID in token"
                    )
                );
            }

            // Allow access if user is admin/provider or requesting their own active assignments
            if (!isAdmin && !isProvider && currentUserId != userId)
            {
                return Forbid();
            }

            _logger.LogInformation("Requesting active assignments for user {UserId}", userId);
            var result = await _assignmentService.GetActiveAssignmentsByUserIdAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active assignments for user {UserId}", userId);
            return StatusCode(
                500,
                ApiResponse<IEnumerable<InventoryAssignmentDto>>.Failure(
                    "An error occurred while retrieving active assignments"
                )
            );
        }
    }

    /// <summary>
    /// Get overdue assignments (Admin or Provider)
    /// </summary>
    /// <returns>List of overdue assignments</returns>
    [HttpGet("overdue")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<InventoryAssignmentDto>>>
    > GetOverdueAssignments()
    {
        try
        {
            _logger.LogInformation("Requesting overdue assignments");
            var result = await _assignmentService.GetOverdueAssignmentsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving overdue assignments");
            return StatusCode(
                500,
                ApiResponse<IEnumerable<InventoryAssignmentDto>>.Failure(
                    "An error occurred while retrieving overdue assignments"
                )
            );
        }
    }

    /// <summary>
    /// Get assignment history for an inventory item (Admin or Provider)
    /// </summary>
    /// <param name="inventoryId">Inventory ID</param>
    /// <returns>Assignment history for the inventory item</returns>
    [HttpGet("history/inventory/{inventoryId:int}")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<ActionResult<ApiResponse<AssignmentHistoryDto>>> GetAssignmentHistory(
        int inventoryId
    )
    {
        try
        {
            _logger.LogInformation(
                "Requesting assignment history for inventory {InventoryId}",
                inventoryId
            );
            var result = await _assignmentService.GetAssignmentHistoryAsync(inventoryId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving assignment history for inventory {InventoryId}",
                inventoryId
            );
            return StatusCode(
                500,
                ApiResponse<AssignmentHistoryDto>.Failure(
                    "An error occurred while retrieving assignment history"
                )
            );
        }
    }

    /// <summary>
    /// Create new assignment (Admin or Provider)
    /// </summary>
    /// <param name="createAssignmentDto">Assignment creation data</param>
    /// <returns>Created assignment details</returns>
    [HttpPost]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<ActionResult<ApiResponse<InventoryAssignmentDto>>> CreateAssignment(
        [FromBody] CreateInventoryAssignmentDto createAssignmentDto
    )
    {
        try
        {
            var userIdClaim = User.FindFirst(AuthConstants.Claims.UserId)?.Value;
            if (
                string.IsNullOrEmpty(userIdClaim)
                || !int.TryParse(userIdClaim, out int assignedByUserId)
            )
            {
                return BadRequest(ApiResponse<InventoryAssignmentDto>.Failure("Invalid user ID"));
            }

            _logger.LogInformation(
                "User {AssignedByUserId} creating assignment for inventory {InventoryId} to user {UserId}",
                assignedByUserId,
                createAssignmentDto.InventoryId,
                createAssignmentDto.UserId
            );

            if (!ModelState.IsValid)
            {
                return BadRequest(
                    ApiResponse<InventoryAssignmentDto>.Failure(
                        "Invalid request data",
                        ModelState
                            .Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                            .ToList()
                    )
                );
            }

            var result = await _assignmentService.CreateAssignmentAsync(
                createAssignmentDto,
                assignedByUserId
            );

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation(
                "Successfully created assignment {AssignmentId}",
                result.Data!.Id
            );
            return CreatedAtAction(nameof(GetAssignmentById), new { id = result.Data!.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating assignment");
            return StatusCode(
                500,
                ApiResponse<InventoryAssignmentDto>.Failure(
                    "An error occurred while creating assignment"
                )
            );
        }
    }

    /// <summary>
    /// Update assignment (Admin or Provider)
    /// </summary>
    /// <param name="id">Assignment ID</param>
    /// <param name="updateAssignmentDto">Assignment update data</param>
    /// <returns>Updated assignment details</returns>
    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<ActionResult<ApiResponse<InventoryAssignmentDto>>> UpdateAssignment(
        int id,
        [FromBody] UpdateInventoryAssignmentDto updateAssignmentDto
    )
    {
        try
        {
            _logger.LogInformation("Updating assignment {AssignmentId}", id);

            if (!ModelState.IsValid)
            {
                return BadRequest(
                    ApiResponse<InventoryAssignmentDto>.Failure(
                        "Invalid request data",
                        ModelState
                            .Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                            .ToList()
                    )
                );
            }

            var result = await _assignmentService.UpdateAssignmentAsync(id, updateAssignmentDto);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating assignment {AssignmentId}", id);
            return StatusCode(
                500,
                ApiResponse<InventoryAssignmentDto>.Failure(
                    "An error occurred while updating assignment"
                )
            );
        }
    }

    /// <summary>
    /// Return assignment (Admin or Provider)
    /// </summary>
    /// <param name="returnAssignmentDto">Return assignment data</param>
    /// <returns>Success status</returns>
    [HttpPost("return")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<ActionResult<ApiResponse<bool>>> ReturnAssignment(
        [FromBody] ReturnInventoryAssignmentDto returnAssignmentDto
    )
    {
        try
        {
            var userIdClaim = User.FindFirst(AuthConstants.Claims.UserId)?.Value;
            if (
                string.IsNullOrEmpty(userIdClaim)
                || !int.TryParse(userIdClaim, out int returnedToUserId)
            )
            {
                return BadRequest(ApiResponse<bool>.Failure("Invalid user ID"));
            }

            _logger.LogInformation(
                "User {ReturnedToUserId} processing return for assignment {AssignmentId}",
                returnedToUserId,
                returnAssignmentDto.AssignmentId
            );

            if (!ModelState.IsValid)
            {
                return BadRequest(
                    ApiResponse<bool>.Failure(
                        "Invalid request data",
                        ModelState
                            .Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                            .ToList()
                    )
                );
            }

            var result = await _assignmentService.ReturnAssignmentAsync(
                returnAssignmentDto,
                returnedToUserId
            );

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation(
                "Successfully processed return for assignment {AssignmentId}",
                returnAssignmentDto.AssignmentId
            );
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing assignment return");
            return StatusCode(
                500,
                ApiResponse<bool>.Failure("An error occurred while processing assignment return")
            );
        }
    }
}
