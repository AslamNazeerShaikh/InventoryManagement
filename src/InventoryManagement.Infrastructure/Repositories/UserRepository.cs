using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Interfaces;
using InventoryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories;

/// <summary>EF Core repository for <see cref="User"/> with authentication-oriented queries.</summary>
public class UserRepository : GenericRepository<User>, IUserRepository
{
    /// <summary>Creates the repository.</summary>
    public UserRepository(AppDbContext appDbContext)
        : base(appDbContext) { }

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<bool> IsEmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default
    ) => await EntitySet.AnyAsync(x => x.Email == email, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<User?> GetByActiveRefreshTokenHashAsync(
        string refreshTokenHash,
        DateTime nowUtc,
        CancellationToken cancellationToken = default
    ) =>
        // Tracked (not AsNoTracking): the caller rotates the token and persists the same instance.
        await EntitySet
            .FirstOrDefaultAsync(
                x =>
                    x.RefreshToken == refreshTokenHash
                    && x.RefreshTokenExpiryTime != null
                    && x.RefreshTokenExpiryTime > nowUtc
                    && x.IsActive,
                cancellationToken
            )
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> GetNursePractitionersAsync(
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AsNoTracking()
            .Where(x => x.Role == Domain.Enums.UserRole.NursePractitioner && x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> GetActiveUsersAsync(
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
