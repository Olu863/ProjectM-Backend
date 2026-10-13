using System.Threading.Tasks;
using ProjectM.Services.DTOs;
using System.Collections.Generic;

namespace ProjectM.Services.Services
{
    public interface IUserService
    {
        Task<UserDto> GetUserByIdAsync(int id);
        Task<UserDto> GetUserByEmailAsync(string email);
        Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);
        Task<UserDto> UpdateUserAsync(int id, UpdateUserDto updateUserDto);
        Task DeleteUserAsync(int id);
        Task<LoginResponseDto> LoginAsync(LoginDto loginDto, string ipAddress, string userAgent);
        Task<LoginResponseDto> RefreshTokenAsync(string refreshToken, string ipAddress, string userAgent);
        Task<bool> RevokeRefreshTokenAsync(string refreshToken, string reason);
        Task<bool> LogoutAsync(string refreshToken);
        Task<bool> LogoutAllDevicesAsync(int userId, string currentRefreshToken);
        Task<int> CleanupExpiredTokensAsync();
        Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
        Task<bool> ActivateUserAsync(int userId);
        Task<bool> DeactivateUserAsync(int userId);
        Task<bool> UpdateLastLoginAsync(int userId);
        Task<bool> AssignRoleAsync(int userId, string role);
        Task<IEnumerable<UserDto>> GetUsersByRoleAsync(string role);
    }
}
