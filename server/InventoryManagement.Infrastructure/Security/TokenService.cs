using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InventoryManagement.Domain.Configuration;
using InventoryManagement.Domain.Constants;
using InventoryManagement.Domain.DTOs;
using InventoryManagement.Domain.Security;
using Microsoft.Extensions.Options;

namespace InventoryManagement.Infrastructure.Security;

/// <summary>
/// Default <see cref="ITokenService"/>. Signs JWT access tokens with the key resolved by
/// <see cref="IJwtSigningKeyProvider"/> (Microsoft IdentityModel), and generates high-entropy
/// refresh tokens whose SHA-256 hash is the only value persisted server-side.
/// </summary>
public sealed class TokenService : ITokenService
{
    private const int RefreshTokenBytes = 32; // 256 bits of entropy.

    private readonly IJwtSigningKeyProvider _signingKeyProvider;
    private readonly IDateTimeProvider _clock;
    private readonly JwtOptions _options;

    /// <summary>Creates the token service.</summary>
    public TokenService(
        IJwtSigningKeyProvider signingKeyProvider,
        IDateTimeProvider clock,
        IOptions<JwtOptions> options
    )
    {
        _signingKeyProvider = signingKeyProvider;
        _clock = clock;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<AccessToken> CreateAccessTokenAsync(
        UserDto user,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = _clock.UtcNow;
        var expires = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(AuthConstants.Claims.Tenant, user.TenantId.ToString()),
            new(AuthConstants.Claims.UserId, user.Id.ToString()),
            new(AuthConstants.Claims.Email, user.Email),
            new(AuthConstants.Claims.Name, user.Name),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
        };

        // Role names (standard role claim) and the user's effective permission codes. Permission
        // claims make authorization fully stateless on the request path.
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in user.Permissions)
        {
            claims.Add(new Claim(AuthConstants.Claims.Permission, permission));
        }

        var signingCredentials = await _signingKeyProvider
            .GetSigningCredentialsAsync(cancellationToken)
            .ConfigureAwait(false);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: signingCredentials
        );

        var value = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessToken(value, expires);
    }

    /// <inheritdoc />
    public RefreshToken CreateRefreshToken()
    {
        var raw = RandomNumberGenerator.GetBytes(RefreshTokenBytes);
        var rawValue = Convert.ToBase64String(raw);
        var expires = _clock.UtcNow.AddDays(_options.RefreshTokenDays);
        return new RefreshToken(rawValue, HashRefreshToken(rawValue), expires);
    }

    /// <inheritdoc />
    public string HashRefreshToken(string rawRefreshToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(rawRefreshToken);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawRefreshToken));
        return Convert.ToBase64String(hash);
    }
}
