using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using ProjectM.API.Authorization;
using ProjectM.Services.DTOs;
using ProjectM.Services.Services;
using FluentValidation;

namespace ProjectM.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;
        private readonly IValidator<CreateUserDto> _createUserValidator;
        private readonly IValidator<ChangePasswordDto> _changePasswordValidator;

        public UsersController(
            IUserService userService, 
            ILogger<UsersController> logger,
            IValidator<CreateUserDto> createUserValidator,
            IValidator<ChangePasswordDto> changePasswordValidator)
        {
            _userService = userService;
            _logger = logger;
            _createUserValidator = createUserValidator;
            _changePasswordValidator = changePasswordValidator;
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetUser(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("GetUser called with invalid ID: {UserId}", id);
                return BadRequest(new { message = "Invalid user ID" });
            }

            // Clients may only read their own profile; staff can look up any user.
            if (!User.IsStaff() && id != User.GetUserId())
            {
                return Forbid();
            }

            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", id);
                return NotFound(new { message = "User not found" });
            }

            return Ok(user);
        }

        /// <summary>
        /// Admin only: list users, optionally filtered by <c>?isActive=false</c> to find registrations
        /// that are waiting for approval.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> GetUsers([FromQuery] bool? isActive)
        {
            var users = await _userService.GetUsersAsync(isActive);
            return Ok(users);
        }

        [HttpPost("register")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Register([FromBody] CreateUserDto createUserDto)
        {
            // Validate input
            if (createUserDto == null)
            {
                _logger.LogWarning("Register called with null data");
                return BadRequest(new { message = "Registration data is required" });
            }

            // Run FluentValidation
            var validationResult = await _createUserValidator.ValidateAsync(createUserDto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage });
                _logger.LogWarning("Registration validation failed for {Email}: {Errors}", createUserDto.Email, string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return BadRequest(new { message = "Validation failed", errors });
            }

            // Public registration must never hand out privileged roles:
            //  - Admin accounts cannot be self-registered at all.
            //  - Client accounts are active immediately.
            //  - Staff accounts (Manager / Technician / Operations) are created INACTIVE and must be
            //    approved by an Admin (POST /api/Users/{id}/activate) before they can sign in.
            var requestedRole = createUserDto.Role?.Trim();
            if (string.Equals(requestedRole, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Rejected public registration with the Admin role for {Email}", createUserDto.Email);
                return BadRequest(new { message = "Admin accounts cannot be created through public registration." });
            }

            var isClientRole = string.Equals(requestedRole, "Client", StringComparison.OrdinalIgnoreCase)
                               || string.Equals(requestedRole, "Clients", StringComparison.OrdinalIgnoreCase);

            _logger.LogInformation("Attempting to register user: {Email}", createUserDto.Email);
            var user = await _userService.CreateUserAsync(createUserDto, isActive: isClientRole);

            if (user == null)
            {
                return BadRequest(new { message = "Unable to create user" });
            }

            _logger.LogInformation("User registered successfully: {UserId}", user.Id);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        [HttpPost("login")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            // Validate input
            if (loginDto == null)
            {
                _logger.LogWarning("Login called with null credentials");
                return BadRequest(new { message = "Email and password are required" });
            }

            if (string.IsNullOrWhiteSpace(loginDto.Email))
            {
                _logger.LogWarning("Login called with empty email");
                return BadRequest(new { message = "Email is required" });
            }

            if (string.IsNullOrWhiteSpace(loginDto.Password))
            {
                _logger.LogWarning("Login called with empty password");
                return BadRequest(new { message = "Password is required" });
            }

            var ipAddress = GetIpAddress();
            var userAgent = GetUserAgent();

            _logger.LogInformation("Login attempt for {Email} from {IpAddress}", loginDto.Email, ipAddress);

            var response = await _userService.LoginAsync(loginDto, ipAddress, userAgent);
            if (response == null)
            {
                _logger.LogWarning("Failed login attempt for {Email} from {IpAddress}", loginDto.Email, ipAddress);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Store refresh token in HTTP-only cookie
            //SetRefreshTokenCookie(response.RefreshToken, response.RefreshTokenExpiresAt);

            _logger.LogInformation("User logged in successfully: {Email}", loginDto.Email);

                                            // Return response including refresh token (backend returns it in-body; consumer stores securely)
            return Ok(new
            {
                response.Token,
                response.RefreshToken,
                response.User,
                response.ExpiresAt,
                response.RefreshTokenExpiresAt
            });
        }

        [HttpPost("login-with-cookie")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> LoginWithCookie([FromBody] LoginDto loginDto)
        {
            // Validate input
            if (loginDto == null)
            {
                _logger.LogWarning("LoginWithCookie called with null credentials");
                return BadRequest(new { message = "Email and password are required" });
            }

            if (string.IsNullOrWhiteSpace(loginDto.Email) || string.IsNullOrWhiteSpace(loginDto.Password))
            {
                _logger.LogWarning("LoginWithCookie called with invalid credentials");
                return BadRequest(new { message = "Email and password are required" });
            }

            var ipAddress = GetIpAddress();
            var userAgent = GetUserAgent();

            _logger.LogInformation("Cookie-based login attempt for {Email} from {IpAddress}", loginDto.Email, ipAddress);

            var response = await _userService.LoginAsync(loginDto, ipAddress, userAgent);
            if (response == null)
            {
                _logger.LogWarning("Failed cookie-based login for {Email} from {IpAddress}", loginDto.Email, ipAddress);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Store refresh token in HTTP-only cookie
            SetRefreshTokenCookie(response.RefreshToken, response.RefreshTokenExpiresAt);

            _logger.LogInformation("User logged in successfully with cookie: {Email}", loginDto.Email);

            // Return only access token and user info (refresh token is in cookie)
            return Ok(new
            {
                response.Token,
                response.User,
                response.ExpiresAt
            });
        }

        [HttpPost("refresh-token")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            // Validate input
            if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                _logger.LogWarning("RefreshToken called with null or empty token");
                return BadRequest(new { message = "Refresh token is required" });
            }

            var ipAddress = GetIpAddress();
            var userAgent = GetUserAgent();

            _logger.LogInformation("Refresh token attempt from {IpAddress}", ipAddress);

            var response = await _userService.RefreshTokenAsync(request.RefreshToken, ipAddress, userAgent);
            if (response == null)
            {
                _logger.LogWarning("Invalid refresh token attempt from {IpAddress}", ipAddress);
                return Unauthorized(new { message = "Invalid or expired refresh token" });
            }

            _logger.LogInformation("Token refreshed successfully for user {UserId}", response.User.Id);
            return Ok(response);
        }

        [HttpPost("refresh-token-cookie")]
        [EnableRateLimiting("auth")]
        public async Task<IActionResult> RefreshTokenFromCookie()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("RefreshTokenFromCookie called without refresh token cookie");
                return Unauthorized(new { message = "No refresh token found" });
            }

            var ipAddress = GetIpAddress();
            var userAgent = GetUserAgent();

            _logger.LogInformation("Cookie-based refresh token attempt from {IpAddress}", ipAddress);

            var response = await _userService.RefreshTokenAsync(refreshToken, ipAddress, userAgent);
            if (response == null)
            {
                _logger.LogWarning("Invalid cookie-based refresh token from {IpAddress}", ipAddress);
                // Clear the invalid cookie
                Response.Cookies.Delete("refreshToken");
                return Unauthorized(new { message = "Invalid or expired refresh token" });
            }

            // Update cookie with new refresh token
            SetRefreshTokenCookie(response.RefreshToken, response.RefreshTokenExpiresAt);

            _logger.LogInformation("Token refreshed successfully via cookie for user {UserId}", response.User.Id);

            // Return only access token and user info
            return Ok(new
            {
                response.Token,
                response.User,
                response.ExpiresAt
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("Logout attempt for user {UserId}", userIdClaim);

            // Get refresh token from cookie first, then from body
            var refreshToken = Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
            {
                // If not in cookie, check request body
                var token = Request.Headers["X-Refresh-Token"].FirstOrDefault();
                if (!string.IsNullOrEmpty(token))
                    refreshToken = token;
            }

            if (!string.IsNullOrEmpty(refreshToken))
            {
                await _userService.LogoutAsync(refreshToken);
                Response.Cookies.Delete("refreshToken");
                _logger.LogInformation("User {UserId} logged out successfully", userIdClaim);
            }
            else
            {
                _logger.LogWarning("Logout called without refresh token for user {UserId}", userIdClaim);
            }

            return Ok(new { message = "Logged out successfully" });
        }

        [HttpPost("logout-all-devices")]
        [Authorize]
        public async Task<IActionResult> LogoutAllDevices()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogWarning("LogoutAllDevices called with invalid user ID claim");
                return Unauthorized(new { message = "Invalid user authentication" });
            }

            _logger.LogInformation("Logout all devices initiated for user {UserId}", userId);

            var refreshToken = Request.Cookies["refreshToken"];
            var result = await _userService.LogoutAllDevicesAsync(userId, refreshToken);

            // Clear the cookie
            Response.Cookies.Delete("refreshToken");

            if (!result)
            {
                _logger.LogWarning("Logout all devices failed for user {UserId}", userId);
                return BadRequest(new { message = "Logout all devices failed" });
            }

            _logger.LogInformation("User {UserId} logged out from all devices successfully", userId);
            return Ok(new { message = "Logged out from all devices successfully" });
        }

        [HttpPost("revoke-token")]
        [Authorize]
        public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                _logger.LogWarning("RevokeToken called with null or empty token");
                return BadRequest(new { message = "Refresh token is required" });
            }

            _logger.LogInformation("Token revocation initiated");

            var result = await _userService.RevokeRefreshTokenAsync(request.RefreshToken, "Revoked by user");
            if (!result)
            {
                _logger.LogWarning("Token revocation failed - token may be invalid or already revoked");
                return BadRequest(new { message = "Token revocation failed. Token may be invalid or already revoked" });
            }

            _logger.LogInformation("Token revoked successfully");
            return Ok(new { message = "Token revoked successfully" });
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            if (id <= 0)
            {
                _logger.LogWarning("UpdateUser called with invalid ID: {UserId}", id);
                return BadRequest(new { message = "Invalid user ID" });
            }

            if (updateUserDto == null)
            {
                _logger.LogWarning("UpdateUser called with null data for user {UserId}", id);
                return BadRequest(new { message = "Update data is required" });
            }

            var isAdmin = User.IsInRole(AppRoles.Admin);
            if (!isAdmin && id != User.GetUserId())
            {
                return Forbid();
            }

            // Only an Admin may change a user's role or active status. For everyone else those fields are
            // ignored, otherwise any user could promote themselves to Admin.
            if (!isAdmin)
            {
                updateUserDto.Role = null;
                updateUserDto.IsActive = null;
            }

            _logger.LogInformation("Update initiated for user {UserId}", id);

            var user = await _userService.UpdateUserAsync(id, updateUserDto);
            if (user == null)
            {
                _logger.LogWarning("User not found for update: {UserId}", id);
                return NotFound(new { message = "User not found" });
            }

            _logger.LogInformation("User {UserId} updated successfully", id);
            return Ok(user);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("DeleteUser called with invalid ID: {UserId}", id);
                return BadRequest(new { message = "Invalid user ID" });
            }

            _logger.LogInformation("Delete initiated for user {UserId}", id);
            await _userService.DeleteUserAsync(id);

            _logger.LogInformation("User {UserId} deleted successfully", id);
            return NoContent();
        }

        [HttpPost("{id}/change-password")]
        [EnableRateLimiting("auth")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto changePasswordDto)
        {
            // Validate input
            if (changePasswordDto == null)
            {
                _logger.LogWarning("ChangePassword called with null data for user {UserId}", id);
                return BadRequest(new { message = "Password change data is required" });
            }

            if (string.IsNullOrWhiteSpace(changePasswordDto.OldPassword) || string.IsNullOrWhiteSpace(changePasswordDto.NewPassword))
            {
                _logger.LogWarning("ChangePassword called with empty passwords for user {UserId}", id);
                return BadRequest(new { message = "Old and new passwords are required" });
            }

            if (id <= 0)
            {
                _logger.LogWarning("ChangePassword called with invalid user ID: {UserId}", id);
                return BadRequest(new { message = "Invalid user ID" });
            }

            // A password can only be changed by its owner (and only with the current password).
            if (id != User.GetUserId())
            {
                return Forbid();
            }

            // Enforce the same password policy as registration (this validator existed but was never called).
            var passwordValidation = await _changePasswordValidator.ValidateAsync(changePasswordDto);
            if (!passwordValidation.IsValid)
            {
                var errors = passwordValidation.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage });
                return BadRequest(new { message = "Validation failed", errors });
            }

            _logger.LogInformation("Password change initiated for user {UserId}", id);

            var result = await _userService.ChangePasswordAsync(id, changePasswordDto.OldPassword, changePasswordDto.NewPassword);
            if (!result)
            {
                _logger.LogWarning("Password change failed for user {UserId}", id);
                return BadRequest(new { message = "Password change failed. Old password may be incorrect" });
            }

            _logger.LogInformation("Password changed successfully for user {UserId}", id);
            return Ok(new { message = "Password changed successfully" });
        }

        [HttpPost("{id}/activate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ActivateUser(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("ActivateUser called with invalid ID: {UserId}", id);
                return BadRequest(new { message = "Invalid user ID" });
            }

            _logger.LogInformation("User activation initiated for {UserId}", id);

            var result = await _userService.ActivateUserAsync(id);
            if (!result)
            {
                _logger.LogWarning("User not found for activation: {UserId}", id);
                return NotFound(new { message = "User not found" });
            }

            _logger.LogInformation("User {UserId} activated successfully", id);
            return Ok(new { message = "User activated successfully" });
        }

        [HttpPost("{id}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("DeactivateUser called with invalid ID: {UserId}", id);
                return BadRequest(new { message = "Invalid user ID" });
            }

            _logger.LogInformation("User deactivation initiated for {UserId}", id);

            var result = await _userService.DeactivateUserAsync(id);
            if (!result)
            {
                _logger.LogWarning("User not found for deactivation: {UserId}", id);
                return NotFound(new { message = "User not found" });
            }

            _logger.LogInformation("User {UserId} deactivated successfully", id);
            return Ok(new { message = "User deactivated successfully" });
        }

        // Helper methods
        private void SetRefreshTokenCookie(string refreshToken, DateTime expires)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = expires,
                SameSite = SameSiteMode.Strict,
                Secure = true, // Requires HTTPS
                Path = "/"
            };

            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }

        private string GetIpAddress()
        {
            // Use the connection's address only. The X-Forwarded-For header can be set to anything by the caller,
            // so it must not be trusted unless ForwardedHeaders middleware is configured with your known proxies
            // (that middleware then corrects RemoteIpAddress for us).
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private string GetUserAgent()
        {
            return Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
        }
    }
}
