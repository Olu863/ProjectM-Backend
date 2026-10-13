using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ProjectM.Core.Entities;
using ProjectM.Infrastructure.Data;
using ProjectM.Services.DTOs;
using BCrypt.Net;

namespace ProjectM.Services.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        // Define valid roles with proper casing
        private static readonly Dictionary<string, string> RoleMapping = new(StringComparer.OrdinalIgnoreCase)
        {
            { "admin", "Admin" },
            { "manager", "Manager" },
            { "technician", "Technician" },
            { "client", "Client" },
            { "clients", "Client" },  // Map plural to singular
            { "operations", "Operations" }
        };

        public UserService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// Normalize role to proper casing (e.g., "operations" -> "Operations")
        /// </summary>
        private string NormalizeRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return role;

            // Try exact match first
            if (RoleMapping.TryGetValue(role, out var normalizedRole))
                return normalizedRole;

            // If not in mapping, capitalize first letter (fallback)
            return char.ToUpper(role[0]) + role.Substring(1).ToLower();
        }

        public async Task<UserDto> GetUserByIdAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return null;

            return MapToUserDto(user);
        }

        public async Task<UserDto> GetUserByEmailAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
                return null;

            return MapToUserDto(user);
        }

        public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
        {
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == createUserDto.Email);
            if (existingUser != null)
                throw new InvalidOperationException("User with this email already exists.");

            var user = new User
            {
                Name = createUserDto.Name,
                Email = createUserDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password),
                Role = NormalizeRole(createUserDto.Role),  // Normalize role casing
                Company = createUserDto.Company,
                Phone = createUserDto.Phone,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return MapToUserDto(user);
        }

        public async Task<UserDto> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return null;

            user.Name = updateUserDto.Name;
            user.Company = updateUserDto.Company;
            user.Phone = updateUserDto.Phone;
            user.IsActive = updateUserDto.IsActive;
            user.Role = NormalizeRole(updateUserDto.Role);  // Normalize role casing
            user.UpdatedAt = DateTime.UtcNow;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return MapToUserDto(user);
        }

        public async Task DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto, string ipAddress, string userAgent)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);
            if (user == null || !user.IsActive)
                return null;

            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                return null;

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            var refreshToken = await GenerateRefreshTokenAsync(user.Id, ipAddress, userAgent);

            var expiryInMinutes = Convert.ToDouble(_configuration["Jwt:ExpiryInMinutes"] ?? "15");
            var refreshTokenExpiryInDays = Convert.ToDouble(_configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7");

            return new LoginResponseDto
            {
                Token = token,
                RefreshToken = refreshToken.Token,
                User = MapToUserDto(user),
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryInMinutes),
                RefreshTokenExpiresAt = refreshToken.ExpiresAt
            };
        }

        public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
                return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ActivateUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateLastLoginAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"];
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];
            var expiryInMinutes = Convert.ToDouble(_configuration["Jwt:ExpiryInMinutes"] ?? "15");

            // Validate configuration values
            if (string.IsNullOrWhiteSpace(jwtKey))
                throw new InvalidOperationException("JWT Key is not configured. Please set Jwt:Key in appsettings.json or environment variables.");

            if (string.IsNullOrWhiteSpace(jwtIssuer))
                throw new InvalidOperationException("JWT Issuer is not configured. Please set Jwt:Issuer in appsettings.json.");

            if (string.IsNullOrWhiteSpace(jwtAudience))
                throw new InvalidOperationException("JWT Audience is not configured. Please set Jwt:Audience in appsettings.json.");

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryInMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<RefreshToken> GenerateRefreshTokenAsync(int userId, string ipAddress, string userAgent)
        {
            var refreshTokenExpiryInDays = Convert.ToDouble(_configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7");

            // Revoke all existing active refresh tokens for this user
            // NOTE: IsActive is a computed property, so we need to use database columns directly
            var existingTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var existingToken in existingTokens)
            {
                existingToken.IsRevoked = true;
                existingToken.RevokedAt = DateTime.UtcNow;
                existingToken.RevokedReason = "Replaced by new token";
            }

            // Generate new refresh token
            var refreshToken = new RefreshToken
            {
                UserId = userId,
                Token = GenerateRefreshTokenString(),
                ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryInDays),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                DeviceId = GenerateDeviceId(ipAddress, userAgent)
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return refreshToken;
        }

        private string GenerateRefreshTokenString()
        {
            var randomNumber = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }
            return Convert.ToBase64String(randomNumber);
        }

        private string GenerateDeviceId(string ipAddress, string userAgent)
        {
            var deviceString = $"{ipAddress}|{userAgent}";
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(deviceString));
                return Convert.ToBase64String(hash);
            }
        }

        private async Task RevokeAllUserTokensAsync(int userId, string reason)
        {
            // NOTE: IsActive is a computed property, use database columns directly
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedReason = reason;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<LoginResponseDto> RefreshTokenAsync(string refreshTokenString, string ipAddress, string userAgent)
        {
            var refreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshTokenString);

            if (refreshToken == null)
                return null;

            var user = refreshToken.User;

            // REUSE DETECTION: If the token is already revoked but someone tries to use it,
            // this indicates a potential token theft attack. Revoke all tokens for this user.
            if (refreshToken.IsRevoked)
            {
                refreshToken.ReusedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Log security event
                Console.WriteLine($"[SECURITY WARNING] Revoked token reuse detected for user {user.Email} from IP {ipAddress}");

                // Revoke all active tokens for this user as a security measure
                await RevokeAllUserTokensAsync(user.Id, "Token reuse detected - potential security breach");

                return null;
            }

            // Check if token is expired
            if (refreshToken.IsExpired)
                return null;

            // Mark the old token with the new token that will replace it
            var newRefreshToken = await GenerateRefreshTokenAsync(user.Id, ipAddress, userAgent);

            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedReason = "Replaced by new token";
            refreshToken.ReplacedByToken = newRefreshToken.Token;

            // Generate new access token
            var newAccessToken = GenerateJwtToken(user);

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var expiryInMinutes = Convert.ToDouble(_configuration["Jwt:ExpiryInMinutes"] ?? "15");

            return new LoginResponseDto
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                User = MapToUserDto(user),
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiryInMinutes),
                RefreshTokenExpiresAt = newRefreshToken.ExpiresAt
            };
        }

        public async Task<bool> RevokeRefreshTokenAsync(string refreshTokenString, string reason)
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshTokenString);

            if (refreshToken == null || !refreshToken.IsActive)
                return false;

            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;
            refreshToken.RevokedReason = reason ?? "Revoked by user";

            await _context.SaveChangesAsync();
            return true;
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                Company = user.Company,
                Phone = user.Phone,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }

        public async Task<bool> AssignRoleAsync(int userId, string role)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.Role = role;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<UserDto>> GetUsersByRoleAsync(string role)
        {
            var users = await _context.Users
                .Where(u => u.Role == role && u.IsActive)  // Fixed: Should return active users
                .ToListAsync();

            return users.Select(MapToUserDto);
        }

        public async Task<bool> LogoutAsync(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                return false;

            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (token == null || !token.IsActive)
                return false;

            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedReason = "Logged out by user";

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LogoutAllDevicesAsync(int userId, string currentRefreshToken)
        {
            // NOTE: IsActive is a computed property, use database columns directly
            var allTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
                .ToListAsync();

            if (!allTokens.Any())
                return false;

            foreach (var token in allTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedReason = "Logged out from all devices";
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> CleanupExpiredTokensAsync()
        {
            var expiredTokens = await _context.RefreshTokens
                .Where(rt => rt.ExpiresAt < DateTime.UtcNow && !rt.IsRevoked)
                .ToListAsync();

            foreach (var token in expiredTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedReason = "Expired and cleaned up";
            }

            if (expiredTokens.Any())
            {
                await _context.SaveChangesAsync();
            }

            // Also delete very old tokens (older than 90 days) to keep database clean
            var cutoffDate = DateTime.UtcNow.AddDays(-90);
            var oldTokens = await _context.RefreshTokens
                .Where(rt => rt.CreatedAt < cutoffDate)
                .ToListAsync();

            if (oldTokens.Any())
            {
                _context.RefreshTokens.RemoveRange(oldTokens);
                await _context.SaveChangesAsync();
            }

            return expiredTokens.Count + oldTokens.Count;
        }
    }
}
