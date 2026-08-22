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
        // Pre-authentication lookup: bypass the tenant filter (the tenant is unknown until the user is
        // resolved) while still excluding soft-deleted accounts. Email is globally unique.
        await EntitySet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.Email == email && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<bool> IsEmailExistsAsync(
        string email,
        CancellationToken cancellationToken = default
    ) =>
        // Email is the global (pre-tenant) login key, so uniqueness is enforced across all tenants;
        // this deliberately bypasses the tenant filter but ignores soft-deleted rows.
        await EntitySet
            .IgnoreQueryFilters()
            .AnyAsync(x => x.Email == email && !x.IsDeleted, cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<User?> GetByActiveRefreshTokenHashAsync(
        string refreshTokenHash,
        DateTime nowUtc,
        CancellationToken cancellationToken = default
    ) =>
        // Tracked (not AsNoTracking): the caller rotates the token and persists the same instance.
        // Cross-tenant by design (the token itself identifies the user), so the tenant filter is
        // bypassed; soft-deleted accounts are still excluded.
        await EntitySet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                x =>
                    x.RefreshToken == refreshTokenHash
                    && x.RefreshTokenExpiryTime != null
                    && x.RefreshTokenExpiryTime > nowUtc
                    && x.IsActive
                    && !x.IsDeleted,
                cancellationToken
            )
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<User>> GetActiveUsersAsync(
        CancellationToken cancellationToken = default
    ) =>
        await EntitySet
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Include(x => x.UserRoles)
            .ThenInclude(ur => ur.Role)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}
