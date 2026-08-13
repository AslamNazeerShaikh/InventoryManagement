using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers;

/// <summary>User management endpoints. Administrative operations are restricted; users may view and
/// update their own profile. Role/admin flags cannot be self-escalated.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = AuthConstants.Policies.AllRoles)]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    /// <summary>Creates the controller.</summary>
    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>Lists all users (Admin only).</summary>
    [HttpGet]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetAllUsers(
        CancellationToken cancellationToken
    ) => Ok(await _userService.GetAllUsersAsync(cancellationToken));

    /// <summary>Lists users with pagination (Admin only).</summary>
    [HttpGet("paged")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsersPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default
    ) => Ok(await _userService.GetUsersPagedAsync(pageNumber, pageSize, cancellationToken));

    /// <summary>Gets a user by identifier (Admin, or the caller's own profile).</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById(
        int id,
        CancellationToken cancellationToken
    )
    {
        if (!TryGetCallerId(out var currentUserId))
        {
            return BadRequest(ApiResponse<UserDto>.Failure("Invalid user ID in token"));
        }

        if (!IsAdmin() && currentUserId != id)
        {
            return Forbid();
        }

        var result = await _userService.GetUserByIdAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    /// <summary>Gets a user by email (Admin only).</summary>
    [HttpGet("by-email/{email}")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUserByEmail(
        string email,
        CancellationToken cancellationToken
    )
    {
        var result = await _userService.GetUserByEmailAsync(email, cancellationToken);
        return result.IsSuccess ? Ok(result) : NotFound(result);
    }

    /// <summary>Creates a new user (Admin only).</summary>
    [HttpPost]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(
        [FromBody] CreateUserDto createUserDto,
        CancellationToken cancellationToken
    )
    {
        var result = await _userService.CreateUserAsync(createUserDto, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetUserById), new { id = result.Data!.Id }, result);
    }

    /// <summary>Updates a user (Admin, or the caller's own profile without privilege escalation).</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(
        int id,
        [FromBody] UpdateUserDto updateUserDto,
        CancellationToken cancellationToken
    )
    {
        if (!TryGetCallerId(out var currentUserId))
        {
            return BadRequest(ApiResponse<UserDto>.Failure("Invalid user ID in token"));
        }

        var isAdmin = IsAdmin();
        if (!isAdmin && currentUserId != id)
        {
            return Forbid();
        }

        // Non-admins cannot alter role/admin/provider flags: preserve the persisted values.
        if (!isAdmin)
        {
            var current = await _userService.GetUserByIdAsync(id, cancellationToken);
            if (current is { IsSuccess: true, Data: not null })
            {
                updateUserDto.Role = current.Data.Role;
                updateUserDto.IsAdmin = current.Data.IsAdmin;
                updateUserDto.IsProvider = current.Data.IsProvider;
            }
        }

        var result = await _userService.UpdateUserAsync(id, updateUserDto, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>Deletes a user (Admin only; cannot delete self).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(
        int id,
        CancellationToken cancellationToken
    )
    {
        if (TryGetCallerId(out var currentUserId) && currentUserId == id)
        {
            return BadRequest(ApiResponse<bool>.Failure("Cannot delete your own account"));
        }

        var result = await _userService.DeleteUserAsync(id, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>Lists nurse-practitioner users (Admin or Provider).</summary>
    [HttpGet("nurse-practitioners")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetNursePractitioners(
        CancellationToken cancellationToken
    ) => Ok(await _userService.GetNursePractitionersAsync(cancellationToken));

    /// <summary>Lists active users (Admin or Provider).</summary>
    [HttpGet("active")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetActiveUsers(
        CancellationToken cancellationToken
    ) => Ok(await _userService.GetActiveUsersAsync(cancellationToken));

    private bool TryGetCallerId(out int userId) =>
        int.TryParse(User.FindFirst(AuthConstants.Claims.UserId)?.Value, out userId);

    private bool IsAdmin() => User.FindFirst(AuthConstants.Claims.IsAdmin)?.Value == "True";
}
