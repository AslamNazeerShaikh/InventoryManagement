using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate user and generate JWT token
    /// </summary>
    /// <param name="loginDto">User login credentials</param>
    /// <returns>JWT token and user information</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(
        [FromBody] LoginDto loginDto
    )
    {
        try
        {
            _logger.LogInformation("Login attempt for email: {Email}", loginDto.Email);

            if (!ModelState.IsValid)
            {
                return BadRequest(
                    ApiResponse<AuthResponseDto>.Failure(
                        "Invalid request data",
                        ModelState
                            .Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                            .ToList()
                    )
                );
            }

            var result = await _authService.LoginAsync(loginDto);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successful login for email: {Email}", loginDto.Email);
                return Ok(result);
            }

            _logger.LogWarning("Failed login attempt for email: {Email}", loginDto.Email);
            return Unauthorized(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", loginDto.Email);
            return StatusCode(
                500,
                ApiResponse<AuthResponseDto>.Failure("An error occurred during login")
            );
        }
    }

    /// <summary>
    /// Refresh JWT token using refresh token
    /// </summary>
    /// <param name="refreshTokenDto">Refresh token</param>
    /// <returns>New JWT token</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken(
        [FromBody] RefreshTokenDto refreshTokenDto
    )
    {
        try
        {
            _logger.LogInformation("Token refresh attempt");

            if (!ModelState.IsValid)
            {
                return BadRequest(
                    ApiResponse<AuthResponseDto>.Failure(
                        "Invalid request data",
                        ModelState
                            .Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                            .ToList()
                    )
                );
            }

            var result = await _authService.RefreshTokenAsync(refreshTokenDto);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successful token refresh");
                return Ok(result);
            }

            _logger.LogWarning("Failed token refresh attempt");
            return Unauthorized(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(
                500,
                ApiResponse<AuthResponseDto>.Failure("An error occurred during token refresh")
            );
        }
    }

    /// <summary>
    /// Logout user and invalidate refresh token
    /// </summary>
    /// <returns>Success status</returns>
    [HttpPost("logout")]
    [Authorize(Policy = AuthConstants.Policies.AllRoles)]
    public async Task<ActionResult<ApiResponse<bool>>> Logout()
    {
        try
        {
            var userIdClaim = User.FindFirst(AuthConstants.Claims.UserId)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return BadRequest(ApiResponse<bool>.Failure("Invalid user ID"));
            }

            _logger.LogInformation("Logout attempt for user ID: {UserId}", userId);

            var result = await _authService.LogoutAsync(userId);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successful logout for user ID: {UserId}", userId);
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, ApiResponse<bool>.Failure("An error occurred during logout"));
        }
    }

    /// <summary>
    /// Change user password
    /// </summary>
    /// <param name="changePasswordDto">Current and new password</param>
    /// <returns>Success status</returns>
    [HttpPost("change-password")]
    [Authorize(Policy = AuthConstants.Policies.AllRoles)]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword(
        [FromBody] ChangePasswordDto changePasswordDto
    )
    {
        try
        {
            var userIdClaim = User.FindFirst(AuthConstants.Claims.UserId)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return BadRequest(ApiResponse<bool>.Failure("Invalid user ID"));
            }

            _logger.LogInformation("Password change attempt for user ID: {UserId}", userId);

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

            var result = await _authService.ChangePasswordAsync(userId, changePasswordDto);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Successful password change for user ID: {UserId}", userId);
                return Ok(result);
            }

            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password change");
            return StatusCode(
                500,
                ApiResponse<bool>.Failure("An error occurred during password change")
            );
        }
    }

    /// <summary>
    /// Get current user information
    /// </summary>
    /// <returns>Current user details</returns>
    [HttpGet("me")]
    [Authorize(Policy = AuthConstants.Policies.AllRoles)]
    public ActionResult<object> GetCurrentUser()
    {
        try
        {
            var userClaims = new
            {
                UserId = User.FindFirst(AuthConstants.Claims.UserId)?.Value,
                Email = User.FindFirst(AuthConstants.Claims.Email)?.Value,
                Name = User.FindFirst(AuthConstants.Claims.Name)?.Value,
                Role = User.FindFirst(AuthConstants.Claims.Role)?.Value,
                IsAdmin = User.FindFirst(AuthConstants.Claims.IsAdmin)?.Value,
                IsProvider = User.FindFirst(AuthConstants.Claims.IsProvider)?.Value,
            };

            return Ok(
                ApiResponse<object>.Success(userClaims, "Current user information retrieved")
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user information");
            return StatusCode(
                500,
                ApiResponse<object>.Failure("An error occurred while retrieving user information")
            );
        }
    }
}
