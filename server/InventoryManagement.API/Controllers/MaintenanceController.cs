using InventoryManagement.API.Infrastructure;
using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers;

/// <summary>Maintenance/calibration schedule endpoints. Reads for all roles; writes and completion for
/// Admin/Provider; deletes for Admin only.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = AuthConstants.Policies.AllRoles)]
public class MaintenanceController : ApiControllerBase
{
    private readonly IMaintenanceService _maintenanceService;

    /// <summary>Creates the controller.</summary>
    public MaintenanceController(IMaintenanceService maintenanceService)
    {
        _maintenanceService = maintenanceService;
    }

    /// <summary>Gets a deterministic page of all schedules (soonest-due first).</summary>
    [HttpGet("paged")]
    public async Task<
        ActionResult<ApiResponse<PagedResult<MaintenanceScheduleDto>>>
    > GetSchedulesPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default
    ) => HandleResult(await _maintenanceService.GetPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>Lists open schedules due within the given number of days (or already overdue).</summary>
    [HttpGet("due")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<MaintenanceScheduleDto>>>
    > GetDueSchedules(
        [FromQuery] int daysAhead = 30,
        CancellationToken cancellationToken = default
    ) => HandleResult(await _maintenanceService.GetDueAsync(daysAhead, cancellationToken));

    /// <summary>Lists all schedules for a single item.</summary>
    [HttpGet("inventory/{inventoryId:int}")]
    public async Task<
        ActionResult<ApiResponse<IEnumerable<MaintenanceScheduleDto>>>
    > GetSchedulesForItem(int inventoryId, CancellationToken cancellationToken) =>
        HandleResult(
            await _maintenanceService.GetByInventoryIdAsync(inventoryId, cancellationToken)
        );

    /// <summary>Gets a schedule by identifier.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<MaintenanceScheduleDto>>> GetScheduleById(
        int id,
        CancellationToken cancellationToken
    ) => HandleResult(await _maintenanceService.GetByIdAsync(id, cancellationToken));

    /// <summary>Creates a maintenance schedule (Admin or Provider).</summary>
    [HttpPost]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<ActionResult<ApiResponse<MaintenanceScheduleDto>>> CreateSchedule(
        [FromBody] CreateMaintenanceScheduleDto createDto,
        CancellationToken cancellationToken
    )
    {
        var result = await _maintenanceService.CreateAsync(createDto, cancellationToken);
        return HandleResult(
            result,
            (value, message) =>
                CreatedAtAction(
                    nameof(GetScheduleById),
                    new { id = value.Id },
                    ApiResponse<MaintenanceScheduleDto>.Success(value, message)
                )
        );
    }

    /// <summary>Updates a maintenance schedule (Admin or Provider).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<ActionResult<ApiResponse<MaintenanceScheduleDto>>> UpdateSchedule(
        int id,
        [FromBody] UpdateMaintenanceScheduleDto updateDto,
        CancellationToken cancellationToken
    ) => HandleResult(await _maintenanceService.UpdateAsync(id, updateDto, cancellationToken));

    /// <summary>Records completion of a schedule, rolling recurring schedules forward (Admin or Provider).</summary>
    [HttpPost("{id:int}/complete")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<ActionResult<ApiResponse<MaintenanceScheduleDto>>> CompleteSchedule(
        int id,
        [FromBody] CompleteMaintenanceDto completeDto,
        CancellationToken cancellationToken
    )
    {
        if (!int.TryParse(User.FindFirst(AuthConstants.Claims.UserId)?.Value, out var userId))
        {
            return BadRequest(ApiResponse<MaintenanceScheduleDto>.Failure("Invalid user ID"));
        }

        return HandleResult(
            await _maintenanceService.CompleteAsync(id, completeDto, userId, cancellationToken)
        );
    }

    /// <summary>Deletes a maintenance schedule (Admin only).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteSchedule(
        int id,
        CancellationToken cancellationToken
    ) => HandleResult(await _maintenanceService.DeleteAsync(id, cancellationToken));
}
