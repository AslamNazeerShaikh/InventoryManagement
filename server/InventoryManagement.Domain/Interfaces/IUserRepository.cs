using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Domain.Interfaces;

/// <summary>Repository for <see cref="User"/> aggregates with authentication-oriented queries.</summary>
public interface IUserRepository : IGenericRepository<User>
{
    /// <summary>Finds an active user by email, or <c>null</c> when none exists. Tracked for mutation.</summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Returns whether a user (deleted or not, per query filter) already uses the email.</summary>
    Task<bool> IsEmailExistsAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Finds an active user whose stored refresh-token hash matches and is unexpired.</summary>
    /// <param name="refreshTokenHash">SHA-256 hash of the client-presented refresh token.</param>
    /// <param name="nowUtc">Current UTC instant used for the expiry comparison.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<User?> GetByActiveRefreshTokenHashAsync(
        string refreshTokenHash,
        DateTime nowUtc,
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists active users ordered by name.</summary>
    Task<IReadOnlyList<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
}
