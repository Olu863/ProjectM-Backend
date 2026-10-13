# Security Improvements - Password Policy & Token Management

## ✅ Implemented Features

### 1. Strong Password Policy (FluentValidation)
Password validation rules are now enforced across the application:

- **Minimum length**: 8 characters
- **Maximum length**: 128 characters
- **Must contain**:
  - At least one uppercase letter (A-Z)
  - At least one lowercase letter (a-z)
  - At least one number (0-9)
  - At least one special character (!@#$%^&*, etc.)

#### Applied to:
- User Registration (`CreateUserDto`)
- Password Change (`ChangePasswordDto`)
- Additional validation: New password must be different from old password

### 2. Refresh Token Implementation
Secure token management with automatic rotation:

- **Access Token**: 15 minutes (short-lived for security)
- **Refresh Token**: 7 days (longer-lived, securely stored in database)
- **Automatic rotation**: Old refresh tokens are revoked when new ones are created
- **Database tracking**: All refresh tokens are tracked with expiry and revocation status

### 3. Updated JWT Configuration
```json
{
  "Jwt": {
	"Key": "",  // Set via environment variables
	"Issuer": "ProjectM",
	"Audience": "ProjectMClient",
	"ExpiryInMinutes": 15,
	"RefreshTokenExpiryInDays": 7
  }
}
```

## 📋 Database Changes

A new `RefreshTokens` table has been added with the following schema:

```sql
CREATE TABLE RefreshTokens (
	Id INT PRIMARY KEY IDENTITY,
	UserId INT NOT NULL,
	Token NVARCHAR(MAX) NOT NULL,
	ExpiresAt DATETIME2 NOT NULL,
	CreatedAt DATETIME2 NOT NULL,
	IsRevoked BIT NOT NULL,
	ReplacedByToken NVARCHAR(MAX),
	RevokedAt DATETIME2,
	RevokedReason NVARCHAR(MAX),
	FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
```

### To Apply Migration:
```bash
dotnet ef database update --project ProjectM.Infrastructure --startup-project ProjectM.API
```

## 🔌 API Endpoints

### Authentication Flow

#### 1. Register User
```http
POST /api/users/register
Content-Type: application/json

{
  "name": "John Doe",
  "email": "john@example.com",
  "password": "SecureP@ss123",  // Must meet password policy
  "role": "Admin",
  "company": "Your Company",
  "phone": "1234567890"
}
```

Response (400 Bad Request if password doesn't meet policy):
```json
{
  "errors": {
	"Password": [
	  "Password must contain at least one uppercase letter",
	  "Password must contain at least one special character"
	]
  }
}
```

#### 2. Login
```http
POST /api/users/login
Content-Type: application/json

{
  "email": "john@example.com",
  "password": "SecureP@ss123"
}
```

Response (200 OK):
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6...",
  "refreshToken": "CfDJ8N7x2W...",
  "user": {
	"id": 1,
	"name": "John Doe",
	"email": "john@example.com",
	"role": "Admin"
  },
  "expiresAt": "2024-01-01T12:15:00Z",
  "refreshTokenExpiresAt": "2024-01-08T12:00:00Z"
}
```

#### 3. Refresh Token
When your access token expires (after 15 minutes), use this endpoint:

```http
POST /api/users/refresh-token
Content-Type: application/json

{
  "refreshToken": "CfDJ8N7x2W..."
}
```

Response (200 OK):
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6...",  // New access token
  "refreshToken": "BgEI9M8y3X...",  // New refresh token (old one revoked)
  "user": { ... },
  "expiresAt": "2024-01-01T12:30:00Z",
  "refreshTokenExpiresAt": "2024-01-08T12:15:00Z"
}
```

#### 4. Revoke Refresh Token (Logout)
```http
POST /api/users/revoke-token
Authorization: Bearer <access-token>
Content-Type: application/json

{
  "refreshToken": "BgEI9M8y3X..."
}
```

Response (200 OK):
```json
{
  "message": "Token revoked successfully"
}
```

### Protected Endpoints
All other endpoints require the `Authorization` header:

```http
GET /api/users/1
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6...
```

## 💻 Frontend Implementation Guide

### Token Management Best Practices

```javascript
// Store tokens securely
localStorage.setItem('accessToken', response.token);
localStorage.setItem('refreshToken', response.refreshToken);
localStorage.setItem('tokenExpiry', response.expiresAt);

// Axios interceptor for automatic token refresh
axios.interceptors.response.use(
  (response) => response,
  async (error) => {
	const originalRequest = error.config;

	// If 401 and we haven't retried yet
	if (error.response?.status === 401 && !originalRequest._retry) {
	  originalRequest._retry = true;

	  try {
		const refreshToken = localStorage.getItem('refreshToken');
		const response = await axios.post('/api/users/refresh-token', {
		  refreshToken
		});

		// Update tokens
		localStorage.setItem('accessToken', response.data.token);
		localStorage.setItem('refreshToken', response.data.refreshToken);
		localStorage.setItem('tokenExpiry', response.data.expiresAt);

		// Retry original request with new token
		originalRequest.headers.Authorization = `Bearer ${response.data.token}`;
		return axios(originalRequest);
	  } catch (refreshError) {
		// Refresh failed, redirect to login
		localStorage.clear();
		window.location.href = '/login';
		return Promise.reject(refreshError);
	  }
	}

	return Promise.reject(error);
  }
);
```

## 🔒 Security Benefits

1. **Reduced attack surface**: 15-minute access token means stolen tokens expire quickly
2. **Password strength**: Enforced complexity reduces brute-force attack success
3. **Token rotation**: Automatic refresh token rotation prevents replay attacks
4. **Revocation tracking**: Database tracks all tokens and their revocation status
5. **Validation feedback**: Clear error messages guide users to create strong passwords

## ⚙️ Configuration

### Environment Variables
Set these in production (already configured in `launchSettings.json` for development):

```bash
Jwt__Key=<your-secure-jwt-key>
Jwt__Issuer=ProjectM
Jwt__Audience=ProjectMClient
Jwt__ExpiryInMinutes=15
Jwt__RefreshTokenExpiryInDays=7
```

### Adjust Token Expiry (Optional)
You can customize the token lifetimes in `appsettings.json` or environment variables:

- **Development/Testing**: Longer access tokens (60 minutes)
- **Production**: Keep short (15 minutes) for maximum security
- **Refresh Token**: 7-30 days depending on your security requirements

## 📝 Password Policy Customization

To modify password rules, edit the validators in:
- `ProjectM.Services/Validators/CreateUserDtoValidator.cs`
- `ProjectM.Services/Validators/ChangePasswordDtoValidator.cs`

Example: Increase minimum length to 12:
```csharp
RuleFor(x => x.Password)
	.MinimumLength(12).WithMessage("Password must be at least 12 characters")
```

## 🚀 Next Steps (Optional Enhancements)

1. **Account Lockout**: Track failed login attempts and temporarily lock accounts
2. **Password History**: Prevent users from reusing recent passwords
3. **Rate Limiting**: Limit login attempts from same IP
4. **Two-Factor Authentication (2FA)**: Add TOTP or SMS verification
5. **Email Verification**: Require email confirmation on registration
6. **Password Reset**: Implement forgot password flow with secure tokens

## 📚 References

- [OWASP Password Guidelines](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)
- [JWT Best Practices](https://tools.ietf.org/html/rfc8725)
- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
