using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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

        public UsersController(
            IUserService userService, 
            ILogger<UsersController> logger,
            IValidator<CreateUserDto> createUserValidator)
        {
            _userService = userService;
            _logger = logger;
            _createUserValidator = createUserValidator;
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetUser(int id)
        {
            try
            {
                if (id <= 0)
                {
                    _logger.LogWarning("GetUser called with invalid ID: {UserId}", id);
                    return BadRequest(new { message = "Invalid user ID" });
                }

                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", id);
                    return NotFound(new { message = "User not found" });
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}: {Message}", id, ex.Message);
                return StatusCode(500, new { message = "An error occurred while retrieving user", details = ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateUserDto createUserDto)
        {
            try
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

                _logger.LogInformation("Attempting to register user: {Email}", createUserDto.Email);
                var user = await _userService.CreateUserAsync(createUserDto);

                _logger.LogInformation("User registered successfully: {UserId}", user.Id);
                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Registration failed for {Email}: {Message}", createUserDto?.Email, ex.Message);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for {Email}: {Message}. InnerException: {Inner}", createUserDto?.Email, ex.Message, ex.InnerException?.Message ?? "None");
                return StatusCode(500, new { message = "An error occurred during registration", details = ex.Message, innerError = ex.InnerException?.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error for {Email}: {Message}\nStack Trace: {StackTrace}", 
                    loginDto?.Email, ex.Message, ex.StackTrace);
                return StatusCode(500, new { message = "An error occurred during login", details = ex.Message });
            }
        }

        [HttpPost("login-with-cookie")]
        public async Task<IActionResult> LoginWithCookie([FromBody] LoginDto loginDto)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cookie-based login error for {Email}: {Message}\nStack Trace: {StackTrace}",
                    loginDto?.Email, ex.Message, ex.StackTrace);
                return StatusCode(500, new { message = "An error occurred during login", details = ex.Message });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Refresh token error: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
                return StatusCode(500, new { message = "An error occurred while refreshing token", details = ex.Message });
            }
        }

        [HttpPost("refresh-token-cookie")]
        public async Task<IActionResult> RefreshTokenFromCookie()
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cookie-based refresh token error: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
                return StatusCode(500, new { message = "An error occurred while refreshing token", details = ex.Message });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Logout attempt for user {UserId}", userIdClaim);

                // Try to get refresh token from cookie first, then from body
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout error: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
                return StatusCode(500, new { message = "An error occurred during logout", details = ex.Message });
            }
        }

        [HttpPost("logout-all-devices")]
        [Authorize]
        public async Task<IActionResult> LogoutAllDevices()
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout all devices error: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
                return StatusCode(500, new { message = "An error occurred while logging out from all devices", details = ex.Message });
            }
        }

        [HttpPost("revoke-token")]
        [Authorize]
        public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequestDto request)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token revocation error: {Message}\nStack Trace: {StackTrace}", ex.Message, ex.StackTrace);
                return StatusCode(500, new { message = "An error occurred while revoking token", details = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto updateUserDto)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update error for user {UserId}: {Message}\nStack Trace: {StackTrace}", id, ex.Message, ex.StackTrace);
                return StatusCode(500, new { message = "An error occurred while updating user", details = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete error for user {UserId}: {Message}\nStack Trace: {StackTrace}", id, ex.Message, ex.StackTrace);
                return StatusCode(500, new { message = "An error occurred while deleting user", details = ex.Message });
            }
        }

        [HttpPost("{id}/change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto changePasswordDto)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password change error for user {UserId}: {Message}\nStack Trace: {StackTrace}", id, ex.Message, ex.StackTrace);
                return StatusCode(500, new { message = "An error occurred while changing password", details = ex.Message });
            }
        }

        [HttpPost("{id}/activate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ActivateUser(int id)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Activation error for user {UserId}: {Message}\nStack Trace: {StackTrace}", id, ex.Message, ex.StackTrace);
                return StatusCode(500, new { message = "An error occurred while activating user", details = ex.Message });
            }
        }

        [HttpPost("{id}/deactivate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Deactivation error for user {UserId}: {Message}\nStack Trace: {StackTrace}", id, ex.Message, ex.StackTrace);
                return StatusCode(500, new { message = "An error occurred while deactivating user", details = ex.Message });
            }
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
            // Check for forwarded IP first (in case of proxy/load balancer)
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                var forwardedIp = Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedIp))
                    return forwardedIp.Split(',')[0].Trim();
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private string GetUserAgent()
        {
            return Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
        }
    }
}
