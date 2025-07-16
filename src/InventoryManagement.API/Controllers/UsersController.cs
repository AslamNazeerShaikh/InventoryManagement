using System.Security.Claims;
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
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Get all users (Admin only)
    /// </summary>
    /// <returns>List of all users</returns>
    [HttpGet]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetAllUsers()
    {
        try
        {
            _logger.LogInformation("Admin requesting all users");
            var result = await _userService.GetAllUsersAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all users");
            return StatusCode(
                500,
                ApiResponse<IEnumerable<UserDto>>.Failure(
                    "An error occurred while retrieving users"
                )
            );
        }
    }

    /// <summary>
    /// Get users with pagination (Admin only)
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <returns>Paginated list of users</returns>
    [HttpGet("paged")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsersPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10
    )
    {
        try
        {
            if (pageSize > BusinessConstants.Pagination.MaxPageSize)
                pageSize = BusinessConstants.Pagination.MaxPageSize;

            _logger.LogInformation(
                "Admin requesting users page {PageNumber} with size {PageSize}",
                pageNumber,
                pageSize
            );
            var result = await _userService.GetUsersPagedAsync(pageNumber, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paged users");
            return StatusCode(
                500,
                ApiResponse<PagedResult<UserDto>>.Failure(
                    "An error occurred while retrieving users"
                )
            );
        }
    }

    /// <summary>
    /// Get user by ID (Admin or own profile)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>User details</returns>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUserById(int id)
    {
        try
        {
            var currentUserIdClaim = User.FindFirst(AuthConstants.Claims.UserId)?.Value;
            var isAdmin = User.FindFirst(AuthConstants.Claims.IsAdmin)?.Value == "True";

            if (!int.TryParse(currentUserIdClaim, out int currentUserId))
            {
                return BadRequest(ApiResponse<UserDto>.Failure("Invalid user ID in token"));
            }

            // Allow access if user is admin or requesting their own profile
            if (!isAdmin && currentUserId != id)
            {
                return Forbid();
            }

            _logger.LogInformation(
                "User {CurrentUserId} requesting user {RequestedUserId}",
                currentUserId,
                id
            );
            var result = await _userService.GetUserByIdAsync(id);

            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return StatusCode(
                500,
                ApiResponse<UserDto>.Failure("An error occurred while retrieving user")
            );
        }
    }

    /// <summary>
    /// Get user by email (Admin only)
    /// </summary>
    /// <param name="email">User email</param>
    /// <returns>User details</returns>
    [HttpGet("by-email/{email}")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetUserByEmail(string email)
    {
        try
        {
            _logger.LogInformation("Admin requesting user by email: {Email}", email);
            var result = await _userService.GetUserByEmailAsync(email);

            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email: {Email}", email);
            return StatusCode(
                500,
                ApiResponse<UserDto>.Failure("An error occurred while retrieving user")
            );
        }
    }

    /// <summary>
    /// Create new user (Admin only)
    /// </summary>
    /// <param name="createUserDto">User creation data</param>
    /// <returns>Created user details</returns>
    [HttpPost]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<UserDto>>> CreateUser(
        [FromBody] CreateUserDto createUserDto
    )
    {
        try
        {
            _logger.LogInformation("Admin creating user with email: {Email}", createUserDto.Email);

            if (!ModelState.IsValid)
            {
                return BadRequest(
                    ApiResponse<UserDto>.Failure(
                        "Invalid request data",
                        ModelState
                            .Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                            .ToList()
                    )
                );
            }

            var result = await _userService.CreateUserAsync(createUserDto);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Successfully created user: {Email}", createUserDto.Email);
            return CreatedAtAction(nameof(GetUserById), new { id = result.Data!.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user with email: {Email}", createUserDto.Email);
            return StatusCode(
                500,
                ApiResponse<UserDto>.Failure("An error occurred while creating user")
            );
        }
    }

    /// <summary>
    /// Update user (Admin or own profile)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="updateUserDto">User update data</param>
    /// <returns>Updated user details</returns>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUser(
        int id,
        [FromBody] UpdateUserDto updateUserDto
    )
    {
        try
        {
            var currentUserIdClaim = User.FindFirst(AuthConstants.Claims.UserId)?.Value;
            var isAdmin = User.FindFirst(AuthConstants.Claims.IsAdmin)?.Value == "True";

            if (!int.TryParse(currentUserIdClaim, out int currentUserId))
            {
                return BadRequest(ApiResponse<UserDto>.Failure("Invalid user ID in token"));
            }

            // Allow update if user is admin or updating their own profile
            if (!isAdmin && currentUserId != id)
            {
                return Forbid();
            }

            // Non-admin users cannot change role/admin status
            if (!isAdmin)
            {
                // Get current user details to preserve role/admin settings
                var currentUser = await _userService.GetUserByIdAsync(id);
                if (currentUser.IsSuccess && currentUser.Data != null)
                {
                    updateUserDto.Role = currentUser.Data.Role;
                    updateUserDto.IsAdmin = currentUser.Data.IsAdmin;
                    updateUserDto.IsProvider = currentUser.Data.IsProvider;
                }
            }

            _logger.LogInformation(
                "User {CurrentUserId} updating user {UpdatedUserId}",
                currentUserId,
                id
            );

            if (!ModelState.IsValid)
            {
                return BadRequest(
                    ApiResponse<UserDto>.Failure(
                        "Invalid request data",
                        ModelState
                            .Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                            .ToList()
                    )
                );
            }

            var result = await _userService.UpdateUserAsync(id, updateUserDto);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(
                500,
                ApiResponse<UserDto>.Failure("An error occurred while updating user")
            );
        }
    }

    /// <summary>
    /// Delete user (Admin only)
    /// </summary>
    /// <param name="id">User ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = AuthConstants.Policies.AdminOnly)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteUser(int id)
    {
        try
        {
            var currentUserIdClaim = User.FindFirst(AuthConstants.Claims.UserId)?.Value;
            if (int.TryParse(currentUserIdClaim, out int currentUserId) && currentUserId == id)
            {
                return BadRequest(ApiResponse<bool>.Failure("Cannot delete your own account"));
            }

            _logger.LogInformation("Admin deleting user {UserId}", id);

            var result = await _userService.DeleteUserAsync(id);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(
                500,
                ApiResponse<bool>.Failure("An error occurred while deleting user")
            );
        }
    }

    /// <summary>
    /// Get all nurse practitioners (Admin or Provider)
    /// </summary>
    /// <returns>List of nurse practitioners</returns>
    [HttpGet("nurse-practitioners")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetNursePractitioners()
    {
        try
        {
            _logger.LogInformation("Requesting nurse practitioners list");
            var result = await _userService.GetNursePractitionersAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving nurse practitioners");
            return StatusCode(
                500,
                ApiResponse<IEnumerable<UserDto>>.Failure(
                    "An error occurred while retrieving nurse practitioners"
                )
            );
        }
    }

    /// <summary>
    /// Get all active users (Admin or Provider)
    /// </summary>
    /// <returns>List of active users</returns>
    [HttpGet("active")]
    [Authorize(Policy = AuthConstants.Policies.AdminOrProvider)]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetActiveUsers()
    {
        try
        {
            _logger.LogInformation("Requesting active users list");
            var result = await _userService.GetActiveUsersAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active users");
            return StatusCode(
                500,
                ApiResponse<IEnumerable<UserDto>>.Failure(
                    "An error occurred while retrieving active users"
                )
            );
        }
    }
}
