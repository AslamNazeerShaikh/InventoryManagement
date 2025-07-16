using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InventoryManagement.Application.Mapping;
using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace InventoryManagement.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginDto loginDto)
    {
        try
        {
            // Find user by email
            var user = await _unitOfWork.Users.GetByEmailWithRolesAsync(loginDto.Email);
            if (user == null)
            {
                return ApiResponse<AuthResponseDto>.Failure("Invalid email or password");
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return ApiResponse<AuthResponseDto>.Failure("Invalid email or password");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                return ApiResponse<AuthResponseDto>.Failure("Account is disabled");
            }

            // Generate tokens
            var userDto = user.ToDto();
            var accessToken = await GenerateJwtTokenAsync(userDto);
            var refreshToken = await GenerateRefreshTokenAsync();

            // Update user refresh token and last login
            await _unitOfWork.Users.UpdateRefreshTokenAsync(
                user.Id,
                refreshToken,
                DateTime.UtcNow.AddDays(7)
            );
            await _unitOfWork.Users.UpdateLastLoginAsync(user.Id, DateTime.UtcNow);
            await _unitOfWork.SaveAsync();

            var authResponse = new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1), // JWT expires in 1 hour
                User = userDto,
            };

            return ApiResponse<AuthResponseDto>.Success(authResponse, "Login successful");
        }
        catch (Exception ex)
        {
            return ApiResponse<AuthResponseDto>.Failure($"Login failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(
        RefreshTokenDto refreshTokenDto
    )
    {
        try
        {
            // Find user with this refresh token
            var user = await _unitOfWork.Users.FirstOrDefaultAsync(x =>
                x.RefreshToken == refreshTokenDto.RefreshToken
                && x.RefreshTokenExpiryTime > DateTime.UtcNow
                && x.IsActive
            );

            if (user == null)
            {
                return ApiResponse<AuthResponseDto>.Failure("Invalid or expired refresh token");
            }

            // Generate new tokens
            var userDto = user.ToDto();
            var newAccessToken = await GenerateJwtTokenAsync(userDto);
            var newRefreshToken = await GenerateRefreshTokenAsync();

            // Update user with new refresh token
            await _unitOfWork.Users.UpdateRefreshTokenAsync(
                user.Id,
                newRefreshToken,
                DateTime.UtcNow.AddDays(7)
            );
            await _unitOfWork.SaveAsync();

            var authResponse = new AuthResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                User = userDto,
            };

            return ApiResponse<AuthResponseDto>.Success(
                authResponse,
                "Token refreshed successfully"
            );
        }
        catch (Exception ex)
        {
            return ApiResponse<AuthResponseDto>.Failure($"Token refresh failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> LogoutAsync(int userId)
    {
        try
        {
            // Clear refresh token
            await _unitOfWork.Users.UpdateRefreshTokenAsync(
                userId,
                string.Empty,
                DateTime.UtcNow.AddDays(-1)
            );
            await _unitOfWork.SaveAsync();

            return ApiResponse<bool>.Success(true, "Logout successful");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.Failure($"Logout failed: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> ChangePasswordAsync(
        int userId,
        ChangePasswordDto changePasswordDto
    )
    {
        try
        {
            if (changePasswordDto.NewPassword != changePasswordDto.ConfirmPassword)
            {
                return ApiResponse<bool>.Failure("New password and confirm password do not match");
            }

            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
            {
                return ApiResponse<bool>.Failure("User not found");
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash))
            {
                return ApiResponse<bool>.Failure("Current password is incorrect");
            }

            // Update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
            _unitOfWork.Users.Update(user);
            await _unitOfWork.SaveAsync();

            return ApiResponse<bool>.Success(true, "Password changed successfully");
        }
        catch (Exception ex)
        {
            return ApiResponse<bool>.Failure($"Password change failed: {ex.Message}");
        }
    }

    public async Task<string> GenerateJwtTokenAsync(UserDto user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(
            _configuration["Jwt:Secret"] ?? "your-secret-key-here-make-it-long"
        );

        var claims = new List<Claim>
        {
            new Claim(AuthConstants.Claims.UserId, user.Id.ToString()),
            new Claim(AuthConstants.Claims.Email, user.Email),
            new Claim(AuthConstants.Claims.Name, user.Name),
            new Claim(AuthConstants.Claims.Role, user.Role.ToString()),
            new Claim(AuthConstants.Claims.IsAdmin, user.IsAdmin.ToString()),
            new Claim(AuthConstants.Claims.IsProvider, user.IsProvider.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
        };

        // Add role-based claims
        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, AuthConstants.Roles.Admin));
        }
        else if (user.IsProvider)
        {
            claims.Add(new Claim(ClaimTypes.Role, AuthConstants.Roles.NursePractitioner));
        }
        else
        {
            claims.Add(new Claim(ClaimTypes.Role, AuthConstants.Roles.Staff));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            ),
            Issuer = _configuration["Jwt:Issuer"] ?? "InventoryManagement",
            Audience = _configuration["Jwt:Audience"] ?? "InventoryManagement",
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public async Task<string> GenerateRefreshTokenAsync()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public async Task<bool> ValidateRefreshTokenAsync(int userId, string refreshToken)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            return user != null
                && user.RefreshToken == refreshToken
                && user.RefreshTokenExpiryTime > DateTime.UtcNow
                && user.IsActive;
        }
        catch
        {
            return false;
        }
    }
}
