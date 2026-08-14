using InventoryManagement.Domain.DTOs;

namespace InventoryManagement.Domain.Security;

/// <summary>Immutable result of issuing an access token.</summary>
/// <param name="Value">The signed, compact JWT string.</param>
/// <param name="ExpiresAtUtc">Absolute UTC expiry of the token.</param>
public readonly record struct AccessToken(string Value, DateTime ExpiresAtUtc);

/// <summary>
/// Immutable result of issuing a refresh token. The <see cref="RawValue"/> is returned to the
/// client exactly once; only the <see cref="Hash"/> is persisted so a database leak cannot be
/// replayed to impersonate the user.
/// </summary>
/// <param name="RawValue">The high-entropy refresh token handed to the client.</param>
/// <param name="Hash">Deterministic SHA-256 hash of <see cref="RawValue"/> for storage/lookup.</param>
/// <param name="ExpiresAtUtc">Absolute UTC expiry of the refresh token.</param>
public readonly record struct RefreshToken(string RawValue, string Hash, DateTime ExpiresAtUtc);

/// <summary>
/// Issues and hashes authentication tokens. Encapsulates all JWT/cryptography concerns so the
/// application layer depends only on this abstraction (no direct third-party token library).
/// </summary>
public interface ITokenService
{
    /// <summary>Creates a signed access token embedding the user's identity and role claims.</summary>
    /// <param name="user">The authenticated user projection.</param>
    /// <param name="cancellationToken">Token used to cancel signing-key resolution.</param>
    Task<AccessToken> CreateAccessTokenAsync(
        UserDto user,
        CancellationToken cancellationToken = default
    );

    /// <summary>Creates a cryptographically-random refresh token together with its storable hash.</summary>
    RefreshToken CreateRefreshToken();

    /// <summary>
    /// Computes the deterministic storage hash for a raw refresh token, enabling constant-time
    /// lookup/validation of a client-presented token against the persisted hash.
    /// </summary>
    /// <param name="rawRefreshToken">The raw token presented by the client.</param>
    string HashRefreshToken(string rawRefreshToken);
}
