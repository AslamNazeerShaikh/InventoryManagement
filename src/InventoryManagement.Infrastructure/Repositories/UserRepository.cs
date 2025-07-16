using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Domain.Interfaces;
using InventoryManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    private readonly AppDbContext _appDbContext;

    public UserRepository(AppDbContext appDbContext)
        : base(appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _appDbContext.Users.FirstOrDefaultAsync(x => x.Email == email)
            ?? throw new ArgumentNullException();
    }

    public async Task<User?> GetByEmailWithRolesAsync(string email)
    {
        return await _appDbContext.Users.FirstOrDefaultAsync(x => x.Email == email && x.IsActive);
    }

    public async Task<bool> IsEmailExistsAsync(string email)
    {
        return await _appDbContext.Users.AnyAsync(x => x.Email == email);
    }

    public async Task<IEnumerable<User>> GetNursePractitionersAsync()
    {
        return await _appDbContext
            .Users.Where(x => x.Role == UserRole.NursePractitioner && x.IsActive)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        return await _appDbContext.Users.Where(x => x.IsActive).OrderBy(x => x.Name).ToListAsync();
    }

    public async Task<User?> ValidateUserCredentialsAsync(string email, string passwordHash)
    {
        return await _appDbContext.Users.FirstOrDefaultAsync(x =>
            x.Email == email && x.PasswordHash == passwordHash && x.IsActive
        );
    }

    public async Task UpdateLastLoginAsync(int userId, DateTime lastLoginTime)
    {
        var user = await _appDbContext.Users.FindAsync(userId);
        if (user != null)
        {
            user.LastLoginAt = lastLoginTime;
            _appDbContext.Users.Update(user);
        }
    }

    public async Task UpdateRefreshTokenAsync(int userId, string refreshToken, DateTime expiryTime)
    {
        var user = await _appDbContext.Users.FindAsync(userId);
        if (user != null)
        {
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = expiryTime;
            _appDbContext.Users.Update(user);
        }
    }
}
