using InventoryManagement.Application.Mapping;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Interfaces;
using InventoryManagement.Domain.Security;
using Microsoft.Extensions.Logging;

namespace InventoryManagement.Application.Services;

/// <summary>
/// Authentication service. Verifies credentials with a Microsoft PBKDF2 hasher, issues JWT access
/// tokens and rotating refresh tokens (persisted only as SHA-256 hashes), and revokes refresh
/// tokens on logout and password change. Unexpected faults propagate to the global handler; only
/// controlled, non-leaking business messages are returned.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<AuthService> _logger;

    /// <summary>Creates the authentication service.</summary>
    public AuthService(
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IPasswordHasher passwordHasher,
        IDateTimeProvider clock,
        ILogger<AuthService> logger
    )
    {
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(
        LoginDto loginDto,
        CancellationToken cancellationToken = default
    )
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(loginDto.Email, cancellationToken)
            .ConfigureAwait(false);

        // Uniform failure for missing user or bad password to prevent account enumeration.
        if (user is null)
        {
            _logger.LogInformation("Login failed for {Email}: user not found.", loginDto.Email);
            return ApiResponse<AuthResponseDto>.Failure("Invalid email or password");
        }

        var verification = _passwordHasher.Verify(user.PasswordHash, loginDto.Password);
        if (verification == PasswordVerificationOutcome.Failed)
        {
            _logger.LogInformation("Login failed for {Email}: bad password.", loginDto.Email);
            return ApiResponse<AuthResponseDto>.Failure("Invalid email or password");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login blocked for {Email}: account disabled.", loginDto.Email);
            return ApiResponse<AuthResponseDto>.Failure("Account is disabled");
        }

        // Transparently upgrade legacy/weaker hashes on successful login.
        if (verification == PasswordVerificationOutcome.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.Hash(loginDto.Password);
        }

        var response = await IssueTokensAsync(user, cancellationToken).ConfigureAwait(false);
        user.LastLoginAt = _clock.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("User {UserId} logged in.", user.Id);
        return ApiResponse<AuthResponseDto>.Success(response, "Login successful");
    }

    /// <inheritdoc />
    public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(
        RefreshTokenDto refreshTokenDto,
        CancellationToken cancellationToken = default
    )
    {
        var presentedHash = _tokenService.HashRefreshToken(refreshTokenDto.RefreshToken);
        var user = await _unitOfWork
            .Users.GetByActiveRefreshTokenHashAsync(presentedHash, _clock.UtcNow, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            _logger.LogWarning("Refresh token rejected: no matching active token.");
            return ApiResponse<AuthResponseDto>.Failure("Invalid or expired refresh token");
        }

        var response = await IssueTokensAsync(user, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Refresh token rotated for user {UserId}.", user.Id);
        return ApiResponse<AuthResponseDto>.Success(response, "Token refreshed successfully");
    }

    /// <inheritdoc />
    public async Task<ApiResponse<bool>> LogoutAsync(
        int userId,
        CancellationToken cancellationToken = default
    )
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (user is null)
        {
            return ApiResponse<bool>.Failure("User not found");
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("User {UserId} logged out.", userId);
        return ApiResponse<bool>.Success(true, "Logout successful");
    }

    /// <inheritdoc />
    public async Task<ApiResponse<bool>> ChangePasswordAsync(
        int userId,
        ChangePasswordDto changePasswordDto,
        CancellationToken cancellationToken = default
    )
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (user is null)
        {
            return ApiResponse<bool>.Failure("User not found");
        }

        if (
            _passwordHasher.Verify(user.PasswordHash, changePasswordDto.CurrentPassword)
            == PasswordVerificationOutcome.Failed
        )
        {
            return ApiResponse<bool>.Failure("Current password is incorrect");
        }

        user.PasswordHash = _passwordHasher.Hash(changePasswordDto.NewPassword);
        // Invalidate existing sessions: a password change revokes the refresh token.
        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Password changed for user {UserId}.", userId);
        return ApiResponse<bool>.Success(true, "Password changed successfully");
    }

    /// <summary>Issues a fresh access/refresh token pair and persists the refresh-token hash on the user.</summary>
    private async Task<AuthResponseDto> IssueTokensAsync(
        Domain.Entities.User user,
        CancellationToken cancellationToken
    )
    {
        var userDto = user.ToDto();
        var accessToken = await _tokenService
            .CreateAccessTokenAsync(userDto, cancellationToken)
            .ConfigureAwait(false);
        var refreshToken = _tokenService.CreateRefreshToken();

        user.RefreshToken = refreshToken.Hash;
        user.RefreshTokenExpiryTime = refreshToken.ExpiresAtUtc;

        return new AuthResponseDto
        {
            AccessToken = accessToken.Value,
            RefreshToken = refreshToken.RawValue,
            ExpiresAt = accessToken.ExpiresAtUtc,
            User = userDto,
        };
    }
}
