using InventoryManagement.API.Infrastructure;
using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers;

/// <summary>Assignment (allocation/return) endpoints. Listing across users requires elevated roles;
/// callers may always access their own assignments.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = AuthConstants.Policies.AllRoles)]
public class InventoryAssignmentsController : ApiControllerBase
{
    private readonly IInventoryAssignmentService _assignmentService;
    private readonly ILogger<InventoryAssignmentsController> _logger;

    /// <summary>Creates the controller.</summary>
    public InventoryAssignmentsController(
        IInventoryAssignmentService assignmentService,
        ILogger<InventoryAssignmentsController> logger
    )
    {
        _assignmentService = assignmentService;
        _logger = logger;
    }

    /// <summary>Lists all assignments (Admin or Provider).</summary>
    [HttpGet]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<InventoryAssignmentDto>>>
    > GetAllAssignments(CancellationToken cancellationToken) =>
        HandleResult(await _assignmentService.GetAllAssignmentsAsync(cancellationToken));

    /// <summary>Lists assignments with pagination (Admin or Provider).</summary>
    [HttpGet("paged")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<
        ActionResult<ApiResponse<PagedResult<InventoryAssignmentDto>>>
    > GetAssignmentsPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default
    ) =>
        HandleResult(
            await _assignmentService.GetAssignmentsPagedAsync(pageNumber, pageSize, cancellationToken)
        );

    /// <summary>Gets an assignment by identifier (Admin/Provider, or the recipient).</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<InventoryAssignmentDto>>> GetAssignmentById(
        int id,
        CancellationToken cancellationToken
    )
    {
        if (!TryGetCallerId(out var currentUserId))
        {
            return BadRequest(
                ApiResponse<InventoryAssignmentDto>.Failure("Invalid user ID in token")
            );
        }

        var result = await _assignmentService.GetAssignmentByIdAsync(id, cancellationToken);
        if (result.IsFailure)
        {
            return HandleResult(result);
        }

        if (!IsAdminOrProvider() && result.Value!.UserId != currentUserId)
        {
            return Forbid();
        }

        return HandleResult(result);
    }

    /// <summary>Lists a user's assignments (Admin/Provider, or the user themselves).</summary>
    [HttpGet("user/{userId:int}")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<InventoryAssignmentDto>>>
    > GetAssignmentsByUserId(int userId, CancellationToken cancellationToken)
    {
        if (!TryGetCallerId(out var currentUserId))
        {
            return BadRequest(
                ApiResponse<IEnumerable<InventoryAssignmentDto>>.Failure("Invalid user ID in token")
            );
        }

        if (!IsAdminOrProvider() && currentUserId != userId)
        {
            return Forbid();
        }

        return HandleResult(
            await _assignmentService.GetAssignmentsByUserIdAsync(userId, cancellationToken)
        );
    }

    /// <summary>Lists the caller's own assignments.</summary>
    [HttpGet("my-assignments")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<InventoryAssignmentDto>>>
    > GetMyAssignments(CancellationToken cancellationToken)
    {
        if (!TryGetCallerId(out var currentUserId))
        {
            return BadRequest(
                ApiResponse<IEnumerable<InventoryAssignmentDto>>.Failure("Invalid user ID in token")
            );
        }

        return HandleResult(
            await _assignmentService.GetAssignmentsByUserIdAsync(currentUserId, cancellationToken)
        );
    }

    /// <summary>Lists active assignments (Admin or Provider).</summary>
    [HttpGet("active")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<InventoryAssignmentDto>>>
    > GetActiveAssignments(CancellationToken cancellationToken) =>
        HandleResult(await _assignmentService.GetActiveAssignmentsAsync(cancellationToken));

    /// <summary>Lists a user's active assignments (Admin/Provider, or the user themselves).</summary>
    [HttpGet("active/user/{userId:int}")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<InventoryAssignmentDto>>>
    > GetActiveAssignmentsByUserId(int userId, CancellationToken cancellationToken)
    {
        if (!TryGetCallerId(out var currentUserId))
        {
            return BadRequest(
                ApiResponse<IEnumerable<InventoryAssignmentDto>>.Failure("Invalid user ID in token")
            );
        }

        if (!IsAdminOrProvider() && currentUserId != userId)
        {
            return Forbid();
        }

        return HandleResult(
            await _assignmentService.GetActiveAssignmentsByUserIdAsync(userId, cancellationToken)
        );
    }

    /// <summary>Lists overdue assignments (Admin or Provider).</summary>
    [HttpGet("overdue")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<InventoryAssignmentDto>>>
    > GetOverdueAssignments(CancellationToken cancellationToken) =>
        HandleResult(await _assignmentService.GetOverdueAssignmentsAsync(cancellationToken));

    /// <summary>Gets the assignment history for an inventory item (Admin or Provider).</summary>
    [HttpGet("history/inventory/{inventoryId:int}")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<ActionResult<ApiResponse<AssignmentHistoryDto>>> GetAssignmentHistory(
        int inventoryId,
        CancellationToken cancellationToken
    ) => HandleResult(await _assignmentService.GetAssignmentHistoryAsync(inventoryId, cancellationToken));

    /// <summary>Creates a new assignment (Admin or Provider).</summary>
    [HttpPost]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<ActionResult<ApiResponse<InventoryAssignmentDto>>> CreateAssignment(
        [FromBody] CreateInventoryAssignmentDto createAssignmentDto,
        CancellationToken cancellationToken
    )
    {
        if (!TryGetCallerId(out var assignedByUserId))
        {
            return BadRequest(ApiResponse<InventoryAssignmentDto>.Failure("Invalid user ID"));
        }

        var result = await _assignmentService.CreateAssignmentAsync(
            createAssignmentDto,
            assignedByUserId,
            cancellationToken
        );
        return HandleResult(
            result,
            (value, message) =>
                CreatedAtAction(
                    nameof(GetAssignmentById),
                    new { id = value.Id },
                    ApiResponse<InventoryAssignmentDto>.Success(value, message)
                )
        );
    }

    /// <summary>Updates an active assignment (Admin or Provider).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<ActionResult<ApiResponse<InventoryAssignmentDto>>> UpdateAssignment(
        int id,
        [FromBody] UpdateInventoryAssignmentDto updateAssignmentDto,
        CancellationToken cancellationToken
    ) =>
        HandleResult(
            await _assignmentService.UpdateAssignmentAsync(
                id,
                updateAssignmentDto,
                cancellationToken
            )
        );

    /// <summary>Processes a return (Admin or Provider).</summary>
    [HttpPost("return")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<ActionResult<ApiResponse<bool>>> ReturnAssignment(
        [FromBody] ReturnInventoryAssignmentDto returnAssignmentDto,
        CancellationToken cancellationToken
    )
    {
        if (!TryGetCallerId(out var returnedToUserId))
        {
            return BadRequest(ApiResponse<bool>.Failure("Invalid user ID"));
        }

        return HandleResult(
            await _assignmentService.ReturnAssignmentAsync(
                returnAssignmentDto,
                returnedToUserId,
                cancellationToken
            )
        );
    }

    /// <summary>Renews/extends an active assignment's expected return date (Admin or Provider).</summary>
    [HttpPost("renew")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<ActionResult<ApiResponse<InventoryAssignmentDto>>> RenewAssignment(
        [FromBody] RenewInventoryAssignmentDto renewAssignmentDto,
        CancellationToken cancellationToken
    ) => HandleResult(await _assignmentService.RenewAssignmentAsync(renewAssignmentDto, cancellationToken));

    /// <summary>Lists active assignments due within the given number of days (Admin or Provider).</summary>
    [HttpGet("due-soon")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<InventoryAssignmentDto>>>
    > GetDueSoonAssignments(
        [FromQuery] int daysAhead = 7,
        CancellationToken cancellationToken = default
    ) => HandleResult(await _assignmentService.GetDueSoonAssignmentsAsync(daysAhead, cancellationToken));

    private bool TryGetCallerId(out int userId) =>
        int.TryParse(User.FindFirst(AuthConstants.Claims.UserId)?.Value, out userId);

    private bool IsAdminOrProvider() =>
        User.FindFirst(AuthConstants.Claims.IsAdmin)?.Value == "True"
        || User.FindFirst(AuthConstants.Claims.IsProvider)?.Value == "True";
}
