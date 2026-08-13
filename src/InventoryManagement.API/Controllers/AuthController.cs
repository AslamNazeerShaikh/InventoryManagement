using InventoryManagement.API.Infrastructure;
using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace InventoryManagement.API.Controllers;

/// <summary>Authentication endpoints: login, token refresh, logout, password change and identity.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    /// <summary>Creates the controller.</summary>
    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>Authenticates a user and returns access and refresh tokens.</summary>
    /// <param name="loginDto">User login credentials.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(ApiServiceCollectionExtensions.AuthRateLimitPolicy)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(
        [FromBody] LoginDto loginDto,
        CancellationToken cancellationToken
    )
    {
        var result = await _authService.LoginAsync(loginDto, cancellationToken);
        return result.IsSuccess ? Ok(result) : Unauthorized(result);
    }

    /// <summary>Exchanges a valid refresh token for a new token pair.</summary>
    /// <param name="refreshTokenDto">The refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(ApiServiceCollectionExtensions.AuthRateLimitPolicy)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> RefreshToken(
        [FromBody] RefreshTokenDto refreshTokenDto,
        CancellationToken cancellationToken
    )
    {
        var result = await _authService.RefreshTokenAsync(refreshTokenDto, cancellationToken);
        return result.IsSuccess ? Ok(result) : Unauthorized(result);
    }

    /// <summary>Revokes the current user's refresh token.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("logout")]
    [Authorize(Policy = AuthConstants.Policies.AllRoles)]
    public async Task<ActionResult<ApiResponse<bool>>> Logout(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return BadRequest(ApiResponse<bool>.Failure("Invalid user ID"));
        }

        var result = await _authService.LogoutAsync(userId, cancellationToken);
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>Changes the current user's password and revokes existing refresh tokens.</summary>
    /// <param name="changePasswordDto">Current and new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("change-password")]
    [Authorize(Policy = AuthConstants.Policies.AllRoles)]
    public async Task<ActionResult<ApiResponse<bool>>> ChangePassword(
        [FromBody] ChangePasswordDto changePasswordDto,
        CancellationToken cancellationToken
    )
    {
        if (!TryGetUserId(out var userId))
        {
            return BadRequest(ApiResponse<bool>.Failure("Invalid user ID"));
        }

        var result = await _authService.ChangePasswordAsync(
            userId,
            changePasswordDto,
            cancellationToken
        );
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    /// <summary>Returns the identity claims of the authenticated caller.</summary>
    [HttpGet("me")]
    [Authorize(Policy = AuthConstants.Policies.AllRoles)]
    public ActionResult<ApiResponse<object>> GetCurrentUser()
    {
        var claims = new
        {
            UserId = User.FindFirst(AuthConstants.Claims.UserId)?.Value,
            Email = User.FindFirst(AuthConstants.Claims.Email)?.Value,
            Name = User.FindFirst(AuthConstants.Claims.Name)?.Value,
            Role = User.FindFirst(AuthConstants.Claims.Role)?.Value,
            IsAdmin = User.FindFirst(AuthConstants.Claims.IsAdmin)?.Value,
            IsProvider = User.FindFirst(AuthConstants.Claims.IsProvider)?.Value,
        };

        return Ok(ApiResponse<object>.Success(claims, "Current user information retrieved"));
    }

    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirst(AuthConstants.Claims.UserId)?.Value, out userId);
}
