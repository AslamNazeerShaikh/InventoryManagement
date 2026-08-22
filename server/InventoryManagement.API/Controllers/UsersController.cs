using InventoryManagement.API.Infrastructure;
using InventoryManagement.API.Infrastructure.Authorization;
using InventoryManagement.Domain.Authorization;
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
[Authorize]
public class UsersController : ApiControllerBase
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
    [HasPermission(Permissions.Users.Manage)]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetAllUsers(
        CancellationToken cancellationToken
    ) => HandleResult(await _userService.GetAllUsersAsync(cancellationToken));

    /// <summary>Lists users with pagination (Admin only).</summary>
    [HttpGet("paged")]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsersPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default
    ) => HandleResult(await _userService.GetUsersPagedAsync(pageNumber, pageSize, cancellationToken));

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

        if (!CanManageUsers() && currentUserId != id)
        {
            return Forbid();
        }

        return HandleResult(await _userService.GetUserByIdAsync(id, cancellationToken));
    }

    /// <summary>Gets a user by email (Admin only).</summary>
    [HttpGet("by-email/{email}")]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUserByEmail(
        string email,
        CancellationToken cancellationToken
    ) => HandleResult(await _userService.GetUserByEmailAsync(email, cancellationToken));

    /// <summary>Creates a new user (Admin only).</summary>
    [HttpPost]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(
        [FromBody] CreateUserDto createUserDto,
        CancellationToken cancellationToken
    )
    {
        var result = await _userService.CreateUserAsync(createUserDto, cancellationToken);
        return HandleResult(
            result,
            (value, message) =>
                CreatedAtAction(
                    nameof(GetUserById),
                    new { id = value.Id },
                    ApiResponse<UserDto>.Success(value, message)
                )
        );
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

        var canManageUsers = HasPermission(Permissions.Users.Manage);
        if (!canManageUsers && currentUserId != id)
        {
            return Forbid();
        }

        // Only privileged callers may change role membership or active status; a self-service profile
        // update preserves them.
        return HandleResult(
            await _userService.UpdateUserAsync(id, updateUserDto, canManageUsers, cancellationToken)
        );
    }

    /// <summary>Deletes a user (Admin only; cannot delete self).</summary>
    [HttpDelete("{id:int}")]
    [HasPermission(Permissions.Users.Manage)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(
        int id,
        CancellationToken cancellationToken
    )
    {
        if (TryGetCallerId(out var currentUserId) && currentUserId == id)
        {
            return BadRequest(ApiResponse<bool>.Failure("Cannot delete your own account"));
        }

        return HandleResult(await _userService.DeleteUserAsync(id, cancellationToken));
    }

    /// <summary>Lists active users, e.g. to select an assignment recipient (requires users.read).</summary>
    [HttpGet("active")]
    [HasPermission(Permissions.Users.Read)]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetActiveUsers(
        CancellationToken cancellationToken
    ) => HandleResult(await _userService.GetActiveUsersAsync(cancellationToken));

    private bool TryGetCallerId(out int userId) =>
        int.TryParse(User.FindFirst(AuthConstants.Claims.UserId)?.Value, out userId);

    private bool CanManageUsers() => HasPermission(Permissions.Users.Manage);
}
