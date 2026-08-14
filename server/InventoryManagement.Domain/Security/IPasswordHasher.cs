namespace InventoryManagement.Domain.Security;

/// <summary>Outcome of verifying a supplied password against a stored hash.</summary>
public enum PasswordVerificationOutcome
{
    /// <summary>The password did not match the stored hash.</summary>
    Failed = 0,

    /// <summary>The password matched the stored hash.</summary>
    Success = 1,

    /// <summary>
    /// The password matched but the stored hash uses outdated parameters and should be
    /// transparently re-hashed and persisted with current parameters.
    /// </summary>
    SuccessRehashNeeded = 2,
}

/// <summary>
/// Abstraction over password hashing/verification. Implemented with a Microsoft-provided,
/// salted, adaptive algorithm (PBKDF2-HMAC-SHA256) so no third-party crypto library is required.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Produces a salted, self-describing hash for the supplied plaintext password.</summary>
    /// <param name="password">The plaintext password. Never logged or persisted.</param>
    /// <returns>An opaque hash string safe to store.</returns>
    string Hash(string password);

    /// <summary>Verifies a plaintext password against a previously produced hash.</summary>
    /// <param name="hashedPassword">The stored hash.</param>
    /// <param name="providedPassword">The plaintext password to verify.</param>
    /// <returns>The verification outcome, including a rehash hint when parameters are outdated.</returns>
    PasswordVerificationOutcome Verify(string hashedPassword, string providedPassword);
}
