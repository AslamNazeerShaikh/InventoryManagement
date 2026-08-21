using InventoryManagement.API.Infrastructure;
using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers;

/// <summary>Managed storage-location endpoints. Reads for all roles; writes for Admin/Provider;
/// deletes for Admin only.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = AuthConstants.Policies.AllRoles)]
public class LocationsController : ApiControllerBase
{
    private readonly ILocationService _locationService;

    /// <summary>Creates the controller.</summary>
    public LocationsController(ILocationService locationService)
    {
        _locationService = locationService;
    }

    /// <summary>Lists locations, optionally only active ones.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<LocationDto>>>> GetLocations(
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default
    ) => HandleResult(await _locationService.GetAllAsync(activeOnly, cancellationToken));

    /// <summary>Gets a deterministic page of locations.</summary>
    [HttpGet("paged")]
    public async Task<ActionResult<ApiResponse<PagedResult<LocationDto>>>> GetLocationsPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default
    ) => HandleResult(await _locationService.GetPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>Gets a location by identifier.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<LocationDto>>> GetLocationById(
        int id,
        CancellationToken cancellationToken
    ) => HandleResult(await _locationService.GetByIdAsync(id, cancellationToken));

    /// <summary>Creates a location (Admin or Provider).</summary>
    [HttpPost]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<ActionResult<ApiResponse<LocationDto>>> CreateLocation(
        [FromBody] CreateLocationDto createDto,
        CancellationToken cancellationToken
    )
    {
        var result = await _locationService.CreateAsync(createDto, cancellationToken);
        return HandleResult(
            result,
            (value, message) =>
                CreatedAtAction(
                    nameof(GetLocationById),
                    new { id = value.Id },
                    ApiResponse<LocationDto>.Success(value, message)
                )
        );
    }

    /// <summary>Updates a location (Admin or Provider).</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<ActionResult<ApiResponse<LocationDto>>> UpdateLocation(
        int id,
        [FromBody] UpdateLocationDto updateDto,
        CancellationToken cancellationToken
    ) => HandleResult(await _locationService.UpdateAsync(id, updateDto, cancellationToken));

    /// <summary>Deletes a location with no children or linked items (Admin only).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteLocation(
        int id,
        CancellationToken cancellationToken
    ) => HandleResult(await _locationService.DeleteAsync(id, cancellationToken));
}
