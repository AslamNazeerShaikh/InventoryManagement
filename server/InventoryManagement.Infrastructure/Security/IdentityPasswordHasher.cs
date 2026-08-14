using InventoryManagement.Domain.Security;
using Microsoft.AspNetCore.Identity;

namespace InventoryManagement.Infrastructure.Security;

/// <summary>
/// <see cref="IPasswordHasher"/> implemented with Microsoft's
/// <see cref="PasswordHasher{TUser}"/> (PBKDF2-HMAC-SHA256, per-password salt, versioned format).
/// This replaces the previous third-party BCrypt dependency with an in-box Microsoft algorithm and
/// surfaces the framework's rehash signal so credentials can be upgraded transparently over time.
/// </summary>
public sealed class IdentityPasswordHasher : IPasswordHasher
{
    // The default PasswordHasher ignores the TUser argument, so a shared sentinel is safe and allocation-free.
    private static readonly object User = new();
    private readonly PasswordHasher<object> _inner;

    /// <summary>Creates the hasher using the framework's current default parameters.</summary>
    public IdentityPasswordHasher()
    {
        _inner = new PasswordHasher<object>();
    }

    /// <inheritdoc />
    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        return _inner.HashPassword(User, password);
    }

    /// <inheritdoc />
    public PasswordVerificationOutcome Verify(string hashedPassword, string providedPassword)
    {
        ArgumentException.ThrowIfNullOrEmpty(hashedPassword);
        ArgumentException.ThrowIfNullOrEmpty(providedPassword);

        return _inner.VerifyHashedPassword(User, hashedPassword, providedPassword) switch
        {
            PasswordVerificationResult.Success => PasswordVerificationOutcome.Success,
            PasswordVerificationResult.SuccessRehashNeeded =>
                PasswordVerificationOutcome.SuccessRehashNeeded,
            _ => PasswordVerificationOutcome.Failed,
        };
    }
}
