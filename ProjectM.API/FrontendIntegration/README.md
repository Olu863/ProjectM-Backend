# Frontend Integration Guide

## Overview
This guide explains how to integrate your frontend application with the ProjectM API authentication system using JWT tokens and HTTP-only cookies.

## Authentication Flow

### 1. Login Flow
```
User Login → API returns Access Token + Sets HTTP-only Cookie (Refresh Token)
Frontend stores Access Token in memory or localStorage
All API requests include Access Token in Authorization header
```

### 2. Token Refresh Flow
```
Access Token expires → API returns 401
Frontend calls /refresh-token-cookie endpoint
API validates refresh token from cookie → Returns new Access Token
Frontend retries original request with new Access Token
```

### 3. Logout Flow
```
User clicks Logout → Call /logout endpoint
API revokes refresh token
Frontend clears Access Token
Redirect to login page
```

## API Endpoints

### Authentication Endpoints

#### Login (Cookie-based)
```http
POST /api/users/login-with-cookie
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "password123"
}

Response:
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
	"id": 1,
	"name": "John Doe",
	"email": "user@example.com",
	"role": "Admin"
  },
  "expiresAt": "2024-01-01T12:15:00Z"
}
```

#### Refresh Token (Cookie-based)
```http
POST /api/users/refresh-token-cookie
(No body required - token comes from cookie)

Response:
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": { ... },
  "expiresAt": "2024-01-01T12:15:00Z"
}
```

#### Logout
```http
POST /api/users/logout
Authorization: Bearer {accessToken}

Response:
{
  "message": "Logged out successfully"
}
```

#### Logout All Devices
```http
POST /api/users/logout-all-devices
Authorization: Bearer {accessToken}

Response:
{
  "message": "Logged out from all devices successfully"
}
```

## Configuration

### CORS Settings
The API is configured to accept requests from these origins:
- `http://localhost:3000` (React default)
- `http://localhost:4200` (Angular default)
- `http://localhost:5173` (Vite default)

⚠️ **Important**: Update `ProjectM.API/Program.cs` lines 24-29 to include your production frontend URL.

### Cookie Settings
- **HttpOnly**: `true` (prevents JavaScript access)
- **Secure**: `true` (requires HTTPS in production)
- **SameSite**: `Strict` (prevents CSRF attacks)

⚠️ **For Development**: If using HTTP (not HTTPS), temporarily set `Secure: false` in development environment.

## Security Best Practices

1. **Never store refresh tokens in localStorage** - They're in HTTP-only cookies
2. **Store access tokens in memory or sessionStorage** - More secure than localStorage
3. **Always use HTTPS in production**
4. **Include credentials in all requests** - Required for cookie-based auth
5. **Handle token expiration gracefully** - Implement automatic refresh

## Framework-Specific Examples

See the following files for implementation examples:
- [React/Next.js Integration](./react-integration.md)
- [Angular Integration](./angular-integration.md)
- [Vue/Nuxt Integration](./vue-integration.md)
- [Vanilla JavaScript](./vanilla-js-integration.md)

## Troubleshooting

### CORS Issues
If you see CORS errors:
1. Verify your frontend URL is in the CORS policy (Program.cs)
2. Ensure you're including `credentials: 'include'` in fetch requests
3. Check that the API is running on HTTPS (or Secure flag is disabled for dev)

### Cookie Not Being Set
1. Verify the API response includes `Set-Cookie` header
2. Check browser console for cookie warnings
3. Ensure same-origin or CORS is properly configured
4. Verify `credentials: 'include'` is set on requests

### 401 Unauthorized After Login
1. Check that the Authorization header is being sent
2. Verify token format: `Authorization: Bearer {token}`
3. Check token hasn't expired (expiresAt field)
4. Verify JWT configuration matches between environments
