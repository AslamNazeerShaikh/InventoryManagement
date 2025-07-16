using InventoryManagement.Domain.Entities;

namespace InventoryManagement.Domain.Interfaces;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByEmailWithRolesAsync(string email);
    Task<bool> IsEmailExistsAsync(string email);
    Task<IEnumerable<User>> GetNursePractitionersAsync();
    Task<IEnumerable<User>> GetActiveUsersAsync();
    Task<User?> ValidateUserCredentialsAsync(string email, string passwordHash);
    Task UpdateLastLoginAsync(int userId, DateTime lastLoginTime);
    Task UpdateRefreshTokenAsync(int userId, string refreshToken, DateTime expiryTime);
}
