# 📘 UserService & UsersController Architecture Guide

## 🏗️ Three-Layer Architecture Overview

Your application follows the **three-layer architecture** pattern:

```
┌─────────────────────────────────────────┐
│  PRESENTATION LAYER (API)               │
│  UsersController                        │  ← HTTP Endpoints (REST API)
│  - Handles HTTP requests/responses      │
│  - Input validation                     │
│  - Logging                              │
│  - Authentication/Authorization         │
└─────────────────────────────────────────┘
					↓
┌─────────────────────────────────────────┐
│  BUSINESS LOGIC LAYER (Services)        │
│  UserService                            │  ← Business Logic
│  - User management                      │
│  - Authentication logic                 │
│  - Password hashing                     │
│  - Token generation                     │
└─────────────────────────────────────────┘
					↓
┌─────────────────────────────────────────┐
│  DATA ACCESS LAYER (Infrastructure)     │
│  ApplicationDbContext (EF Core)         │  ← Database Operations
│  - CRUD operations                      │
│  - Queries                              │
│  - Transactions                         │
└─────────────────────────────────────────┘
```

---

## 🎯 UsersController (Presentation Layer)

**Location:** `ProjectM.API/Controllers/UsersController.cs`

### **Role & Responsibilities**

The controller is the **API endpoint layer** that handles HTTP communication:

#### ✅ **What It DOES:**
1. **Receives HTTP Requests** from frontend/clients
2. **Validates Input** (null checks, required fields)
3. **Logs Operations** (info, warnings, errors)
4. **Calls UserService** for business logic
5. **Returns HTTP Responses** (200, 400, 401, 500, etc.)
6. **Handles Exceptions** with user-friendly messages
7. **Enforces Security** (`[Authorize]` attributes)

#### ❌ **What It DOES NOT DO:**
- ❌ Direct database access
- ❌ Password hashing
- ❌ Business logic decisions
- ❌ Token generation
- ❌ Data transformations

### **Key Features**

```csharp
[ApiController]                          // Makes it a REST API controller
[Route("api/[controller]")]              // Creates route: /api/users
public class UsersController : ControllerBase
{
	private readonly IUserService _userService;    // Business logic
	private readonly ILogger<UsersController> _logger;  // Logging

	// Constructor injection (Dependency Injection)
	public UsersController(IUserService userService, ILogger<UsersController> logger)
	{
		_userService = userService;
		_logger = logger;
	}
}
```

### **Example Endpoint Breakdown**

#### **Login Endpoint:**
```csharp
[HttpPost("login")]  // Route: POST /api/users/login
public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
{
	try
	{
		// 1. INPUT VALIDATION
		if (loginDto == null)
			return BadRequest(new { message = "Email and password are required" });

		if (string.IsNullOrWhiteSpace(loginDto.Email))
			return BadRequest(new { message = "Email is required" });

		// 2. EXTRACT CONTEXT
		var ipAddress = GetIpAddress();   // From HTTP headers
		var userAgent = GetUserAgent();   // From HTTP headers

		// 3. LOG THE ATTEMPT
		_logger.LogInformation("Login attempt for {Email} from {IpAddress}", 
			loginDto.Email, ipAddress);

		// 4. CALL BUSINESS LOGIC (UserService)
		var response = await _userService.LoginAsync(loginDto, ipAddress, userAgent);

		// 5. CHECK RESULT
		if (response == null)
		{
			_logger.LogWarning("Failed login for {Email}", loginDto.Email);
			return Unauthorized(new { message = "Invalid email or password" });
		}

		// 6. SUCCESS RESPONSE
		_logger.LogInformation("User logged in: {Email}", loginDto.Email);
		return Ok(new
		{
			response.Token,
			response.User,
			response.ExpiresAt
		});
	}
	catch (Exception ex)
	{
		// 7. ERROR HANDLING
		_logger.LogError(ex, "Login error: {Message}", ex.Message);
		return StatusCode(500, new { message = "An error occurred during login" });
	}
}
```

### **All Controller Endpoints**

| HTTP Method | Route | Authorization | Purpose |
|-------------|-------|---------------|---------|
| `GET` | `/api/users/{id}` | ✅ Required | Get user by ID |
| `POST` | `/api/users/register` | ❌ Public | Create new user |
| `POST` | `/api/users/login` | ❌ Public | Login with credentials |
| `POST` | `/api/users/login-with-cookie` | ❌ Public | Login (cookie-based) |
| `POST` | `/api/users/refresh-token` | ❌ Public | Refresh JWT token |
| `POST` | `/api/users/refresh-token-cookie` | ❌ Public | Refresh (cookie-based) |
| `POST` | `/api/users/logout` | ✅ Required | Logout current device |
| `POST` | `/api/users/logout-all-devices` | ✅ Required | Logout all devices |
| `POST` | `/api/users/revoke-token` | ✅ Required | Revoke refresh token |
| `PUT` | `/api/users/{id}` | ✅ Required | Update user |
| `DELETE` | `/api/users/{id}` | ✅ Admin Only | Deactivate user |
| `POST` | `/api/users/{id}/change-password` | ✅ Required | Change password |
| `POST` | `/api/users/{id}/activate` | ✅ Admin Only | Activate user |
| `POST` | `/api/users/{id}/deactivate` | ✅ Admin Only | Deactivate user |

---

## ⚙️ UserService (Business Logic Layer)

**Location:** `ProjectM.Services/Services/UserService.cs`

### **Role & Responsibilities**

The service contains the **core business logic** for user management:

#### ✅ **What It DOES:**
1. **Database Operations** (via EF Core)
2. **Password Security** (BCrypt hashing)
3. **JWT Token Generation**
4. **Refresh Token Management**
5. **User Validation Logic**
6. **Data Transformations** (Entity ↔ DTO)
7. **Business Rules Enforcement**

#### ❌ **What It DOES NOT DO:**
- ❌ HTTP handling
- ❌ Input validation (controller does this)
- ❌ Logging (controller does this)
- ❌ Error formatting for clients

### **Key Dependencies**

```csharp
public class UserService : IUserService
{
	private readonly ApplicationDbContext _context;   // Database access
	private readonly IConfiguration _configuration;   // App settings (JWT keys, etc.)

	public UserService(ApplicationDbContext context, IConfiguration configuration)
	{
		_context = context;
		_configuration = configuration;
	}
}
```

### **Core Methods**

#### **1. User CRUD Operations**

```csharp
// GET - Retrieve user
public async Task<UserDto> GetUserByIdAsync(int id)
{
	var user = await _context.Users.FindAsync(id);
	return user == null ? null : MapToUserDto(user);
}

// CREATE - Register new user
public async Task<UserDto> CreateUserAsync(CreateUserDto createUserDto)
{
	// Check if email already exists
	var existingUser = await _context.Users
		.FirstOrDefaultAsync(u => u.Email == createUserDto.Email);

	if (existingUser != null)
		throw new InvalidOperationException("User with this email already exists.");

	// Hash password with BCrypt
	var user = new User
	{
		Name = createUserDto.Name,
		Email = createUserDto.Email,
		PasswordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password),  // 🔒 Security
		Role = createUserDto.Role,
		IsActive = true
	};

	_context.Users.Add(user);
	await _context.SaveChangesAsync();
	return MapToUserDto(user);
}

// UPDATE - Modify user
public async Task<UserDto> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
{
	var user = await _context.Users.FindAsync(id);
	if (user == null) return null;

	user.Name = updateUserDto.Name;
	user.Phone = updateUserDto.Phone;
	user.UpdatedAt = DateTime.UtcNow;

	await _context.SaveChangesAsync();
	return MapToUserDto(user);
}

// DELETE - Soft delete (deactivate)
public async Task DeleteUserAsync(int id)
{
	var user = await _context.Users.FindAsync(id);
	if (user != null)
	{
		user.IsActive = false;  // Soft delete
		await _context.SaveChangesAsync();
	}
}
```

#### **2. Authentication & Security**

```csharp
// LOGIN - Verify credentials and generate token
public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto, string ipAddress, string userAgent)
{
	// 1. Find user by email
	var user = await _context.Users
		.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

	if (user == null || !user.IsActive)
		return null;  // User not found or inactive

	// 2. Verify password with BCrypt
	if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
		return null;  // Wrong password

	// 3. Update last login timestamp
	user.LastLoginAt = DateTime.UtcNow;
	await _context.SaveChangesAsync();

	// 4. Generate JWT token
	var token = GenerateJwtToken(user);

	// 5. Return login response
	return new LoginResponseDto
	{
		Token = token,
		User = MapToUserDto(user),
		ExpiresAt = DateTime.UtcNow.AddMinutes(15)
	};
}

// CHANGE PASSWORD - Verify old password and set new one
public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
{
	var user = await _context.Users.FindAsync(userId);
	if (user == null) return false;

	// Verify old password
	if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
		return false;

	// Hash and save new password
	user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
	await _context.SaveChangesAsync();
	return true;
}
```

#### **3. JWT Token Generation**

```csharp
private string GenerateJwtToken(User user)
{
	// Get configuration values
	var jwtKey = _configuration["Jwt:Key"];
	var jwtIssuer = _configuration["Jwt:Issuer"];
	var jwtAudience = _configuration["Jwt:Audience"];

	// Validate configuration
	if (string.IsNullOrWhiteSpace(jwtKey))
		throw new InvalidOperationException("JWT Key is not configured");

	// Create signing credentials
	var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
	var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

	// Create claims (user information in token)
	var claims = new[]
	{
		new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
		new Claim(ClaimTypes.Email, user.Email),
		new Claim(ClaimTypes.Name, user.Name),
		new Claim(ClaimTypes.Role, user.Role),
		new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
	};

	// Create JWT token
	var token = new JwtSecurityToken(
		issuer: jwtIssuer,
		audience: jwtAudience,
		claims: claims,
		expires: DateTime.UtcNow.AddMinutes(15),
		signingCredentials: credentials
	);

	// Convert to string
	return new JwtSecurityTokenHandler().WriteToken(token);
}
```

#### **4. User Activation/Deactivation**

```csharp
public async Task<bool> ActivateUserAsync(int userId)
{
	var user = await _context.Users.FindAsync(userId);
	if (user == null) return false;

	user.IsActive = true;
	await _context.SaveChangesAsync();
	return true;
}

public async Task<bool> DeactivateUserAsync(int userId)
{
	var user = await _context.Users.FindAsync(userId);
	if (user == null) return false;

	user.IsActive = false;
	await _context.SaveChangesAsync();
	return true;
}
```

---

## 🔄 How They Work Together

### **Example: Login Flow**

```
┌─────────────┐
│  Frontend   │  1. POST /api/users/login
│             │     { email: "user@example.com", password: "pass123" }
└─────────────┘
	   ↓
┌─────────────────────────────────────────┐
│  UsersController.Login()                │
├─────────────────────────────────────────┤
│  ✓ Validate input (email, password)     │  2. Input validation
│  ✓ Extract IP address & user agent      │  3. Get request context
│  ✓ Log login attempt                    │  4. Log for security
│  ✓ Call: _userService.LoginAsync(...)   │  5. Delegate to service
└─────────────────────────────────────────┘
	   ↓
┌─────────────────────────────────────────┐
│  UserService.LoginAsync()               │
├─────────────────────────────────────────┤
│  ✓ Query database for user by email     │  6. Database lookup
│  ✓ Verify password with BCrypt          │  7. Password check
│  ✓ Update last login timestamp          │  8. Track login
│  ✓ Generate JWT token                   │  9. Create token
│  ✓ Return LoginResponseDto              │  10. Return data
└─────────────────────────────────────────┘
	   ↓
┌─────────────────────────────────────────┐
│  UsersController.Login()                │
├─────────────────────────────────────────┤
│  ✓ Check if response is null            │  11. Validate result
│  ✓ Log success                          │  12. Log outcome
│  ✓ Return HTTP 200 OK with token        │  13. Send response
└─────────────────────────────────────────┘
	   ↓
┌─────────────┐
│  Frontend   │  14. Receive token
│             │     { token: "eyJhbG...", user: {...}, expiresAt: "..." }
└─────────────┘
```

---

## 📦 Data Transfer Objects (DTOs)

DTOs are used to transfer data between layers:

### **CreateUserDto** (Register request)
```csharp
{
	"name": "John Doe",
	"email": "john@example.com",
	"password": "SecurePass123!",
	"role": "User",
	"company": "ACME Corp",
	"phone": "+1234567890"
}
```

### **LoginDto** (Login request)
```csharp
{
	"email": "john@example.com",
	"password": "SecurePass123!"
}
```

### **UserDto** (Response - no password)
```csharp
{
	"id": 1,
	"name": "John Doe",
	"email": "john@example.com",
	"role": "User",
	"company": "ACME Corp",
	"phone": "+1234567890",
	"isActive": true
}
```

### **LoginResponseDto** (Login response)
```csharp
{
	"token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
	"user": { /* UserDto */ },
	"expiresAt": "2024-01-15T15:30:00Z"
}
```

---

## 🔒 Security Features

### **1. Password Security**
- ✅ **BCrypt hashing** (industry standard)
- ✅ Salt automatically generated
- ✅ Password never stored in plain text
- ✅ Password never returned in API responses

### **2. JWT Authentication**
- ✅ Stateless authentication
- ✅ Token expires after 15 minutes
- ✅ Contains user claims (ID, email, role)
- ✅ Signed with secret key

### **3. Authorization**
- ✅ `[Authorize]` - Requires valid JWT
- ✅ `[Authorize(Roles = "Admin")]` - Admin only
- ✅ Role-based access control

### **4. Logging**
- ✅ Login attempts logged with IP address
- ✅ Failed logins tracked
- ✅ Errors logged with stack traces
- ✅ Security-relevant actions logged

---

## 🎓 Key Takeaways

### **UsersController:**
- **HTTP Interface** - Talks to the outside world
- **Thin layer** - Minimal logic
- **Validation & Logging** - Input checks and operation tracking
- **Error handling** - User-friendly error messages

### **UserService:**
- **Business Logic** - Where the real work happens
- **Database access** - All CRUD operations
- **Security** - Password hashing, token generation
- **Reusable** - Can be used by multiple controllers

### **Separation of Concerns:**
| Concern | Who Handles It |
|---------|----------------|
| HTTP requests/responses | **Controller** |
| Input validation | **Controller** |
| Logging | **Controller** |
| Business logic | **Service** |
| Database operations | **Service** |
| Password security | **Service** |
| Token generation | **Service** |

---

**This architecture provides:**
- ✅ **Testability** - Service can be tested independently
- ✅ **Maintainability** - Clear responsibilities
- ✅ **Reusability** - Service can be used by multiple controllers
- ✅ **Security** - Layered security approach
- ✅ **Scalability** - Easy to extend with new features
