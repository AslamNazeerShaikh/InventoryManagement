using InventoryManagement.Domain.DTOs;

namespace InventoryManagement.Domain.Interfaces;

public interface IUserService
{
    Task<ApiResponse<IEnumerable<UserDto>>> GetAllUsersAsync();
    Task<ApiResponse<UserDto>> GetUserByIdAsync(int id);
    Task<ApiResponse<UserDto>> GetUserByEmailAsync(string email);
    Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto createUserDto);
    Task<ApiResponse<UserDto>> UpdateUserAsync(int id, UpdateUserDto updateUserDto);
    Task<ApiResponse<bool>> DeleteUserAsync(int id);
    Task<ApiResponse<IEnumerable<UserDto>>> GetNursePractitionersAsync();
    Task<ApiResponse<IEnumerable<UserDto>>> GetActiveUsersAsync();
    Task<ApiResponse<PagedResult<UserDto>>> GetUsersPagedAsync(int pageNumber, int pageSize);
}
