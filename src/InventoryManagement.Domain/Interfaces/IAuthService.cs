using InventoryManagement.Domain.DTOs;

namespace InventoryManagement.Domain.Interfaces;

/// <summary>Authentication and credential-management operations.</summary>
public interface IAuthService
{
    /// <summary>Validates credentials and issues access + refresh tokens.</summary>
    Task<ApiResponse<AuthResponseDto>> LoginAsync(
        LoginDto loginDto,
        CancellationToken cancellationToken = default
    );

    /// <summary>Rotates tokens given a valid, unexpired refresh token.</summary>
    Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(
        RefreshTokenDto refreshTokenDto,
        CancellationToken cancellationToken = default
    );

    /// <summary>Revokes the current refresh token for the user.</summary>
    Task<ApiResponse<bool>> LogoutAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Changes a user's password and revokes existing refresh tokens.</summary>
    Task<ApiResponse<bool>> ChangePasswordAsync(
        int userId,
        ChangePasswordDto changePasswordDto,
        CancellationToken cancellationToken = default
    );
}
