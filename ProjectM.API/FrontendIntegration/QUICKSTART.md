# Quick Start Guide - Frontend JWT Integration

## 📋 Prerequisites

1. Update `appsettings.json` with your JWT secret key
2. Update CORS origins in `Program.cs` to match your frontend URL
3. Ensure your API is running (typically on `https://localhost:7xxx`)

## ⚙️ Required Configuration

### 1. Update JWT Secret Key

Edit `ProjectM.API/appsettings.json`:

```json
{
  "Jwt": {
	"Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
	"Issuer": "ProjectM",
	"Audience": "ProjectMClient",
	"ExpiryInMinutes": 15,
	"RefreshTokenExpiryInDays": 7
  }
}
```

⚠️ **Important**: The Key must be at least 32 characters for HS256 algorithm.

### 2. Update CORS Origins

Edit `ProjectM.API/Program.cs` (lines 24-29):

```csharp
policy.WithOrigins(
	"http://localhost:3000",      // Your React app
	"http://localhost:5173",      // Your Vite app
	"https://yourdomain.com"      // Your production domain
)
```

### 3. Database Connection

Update connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ProjectM;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

Then apply migrations:

```bash
cd ProjectM.Infrastructure
dotnet ef database update --startup-project ../ProjectM.API
```

## 🚀 Quick Implementation

### For React/Next.js

1. Install dependencies:
```bash
npm install axios
```

2. Create API client:
```javascript
import axios from 'axios';

const api = axios.create({
  baseURL: 'https://localhost:7xxx/api',
  withCredentials: true  // IMPORTANT!
});

// Add token to requests
api.interceptors.request.use(config => {
  const token = sessionStorage.getItem('accessToken');
  if (token) {
	config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export default api;
```

3. Login:
```javascript
const login = async (email, password) => {
  const response = await api.post('/users/login-with-cookie', {
	email,
	password
  });

  sessionStorage.setItem('accessToken', response.data.token);
  return response.data;
};
```

4. Make authenticated requests:
```javascript
const getUserData = async (userId) => {
  const response = await api.get(`/users/${userId}`);
  return response.data;
};
```

### For Vanilla JavaScript

1. Login:
```javascript
async function login(email, password) {
  const response = await fetch('https://localhost:7xxx/api/users/login-with-cookie', {
	method: 'POST',
	headers: { 'Content-Type': 'application/json' },
	credentials: 'include',  // IMPORTANT!
	body: JSON.stringify({ email, password })
  });

  const data = await response.json();
  sessionStorage.setItem('accessToken', data.token);
  return data;
}
```

2. Make authenticated requests:
```javascript
async function fetchData(url) {
  const token = sessionStorage.getItem('accessToken');

  const response = await fetch(url, {
	headers: {
	  'Authorization': `Bearer ${token}`,
	  'Content-Type': 'application/json'
	},
	credentials: 'include'
  });

  return response.json();
}
```

## 🔑 Available Endpoints

| Endpoint | Method | Description | Auth Required |
|----------|--------|-------------|---------------|
| `/api/users/login-with-cookie` | POST | Login (sets cookie) | No |
| `/api/users/refresh-token-cookie` | POST | Refresh token | No |
| `/api/users/logout` | POST | Logout current device | Yes |
| `/api/users/logout-all-devices` | POST | Logout all devices | Yes |
| `/api/users/{id}` | GET | Get user by ID | Yes |
| `/api/users/{id}` | PUT | Update user | Yes |
| `/api/users/{id}/change-password` | POST | Change password | Yes |

## 📝 Example Login Request

```bash
curl -X POST https://localhost:7xxx/api/users/login-with-cookie \
  -H "Content-Type: application/json" \
  -d '{
	"email": "admin@example.com",
	"password": "yourpassword"
  }' \
  --cookie-jar cookies.txt
```

Response:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
	"id": 1,
	"name": "Admin User",
	"email": "admin@example.com",
	"role": "Admin"
  },
  "expiresAt": "2024-01-01T12:15:00Z"
}
```

## 📝 Example Authenticated Request

```bash
curl -X GET https://localhost:7xxx/api/users/1 \
  -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." \
  --cookie cookies.txt
```

## 🔐 Security Checklist

- ✅ Use HTTPS in production
- ✅ Store access token in sessionStorage (not localStorage)
- ✅ Refresh token is in HTTP-only cookie (automatic)
- ✅ Include `credentials: 'include'` in all fetch requests
- ✅ Add `withCredentials: true` for axios requests
- ✅ Update CORS origins to match your domain
- ✅ Generate a strong JWT secret key (32+ characters)
- ✅ Set Secure flag to true in production

## 🐛 Common Issues

### CORS Error
**Error**: "CORS policy: No 'Access-Control-Allow-Origin'"
**Fix**: Add your frontend URL to CORS policy in `Program.cs`

### Cookie Not Set
**Error**: Refresh token cookie not being set
**Fix**: 
1. Ensure `credentials: 'include'` is in your request
2. Check CORS allows credentials
3. Verify Secure flag matches your protocol (HTTP/HTTPS)

### 401 Unauthorized
**Error**: Getting 401 on API requests
**Fix**:
1. Verify token is being sent in `Authorization: Bearer {token}` header
2. Check token hasn't expired
3. Verify JWT Key matches between environments

### Invalid Token
**Error**: "Invalid or expired refresh token"
**Fix**:
1. Clear cookies and sessionStorage
2. Login again
3. Check database migration was applied

## 📚 Full Documentation

For complete integration examples, see:
- `/ProjectM.API/FrontendIntegration/react-integration.md` - React/Next.js
- `/ProjectM.API/FrontendIntegration/vanilla-js-integration.md` - Plain JavaScript
- `/ProjectM.API/FrontendIntegration/README.md` - Complete guide

## 🧪 Testing Your Integration

1. **Test Login**:
   - Send login request
   - Verify token in response
   - Check cookie is set in browser DevTools

2. **Test Authenticated Request**:
   - Use received token in Authorization header
   - Verify request succeeds

3. **Test Token Refresh**:
   - Wait for token to expire (~15 min)
   - Make request → should auto-refresh
   - OR manually call refresh endpoint

4. **Test Logout**:
   - Call logout endpoint
   - Verify cookie is cleared
   - Verify subsequent requests fail

## 💡 Pro Tips

1. **Auto-Refresh**: Implement interceptor to auto-refresh before expiration
2. **Loading States**: Show loading spinner during auth operations
3. **Error Handling**: Display user-friendly messages for auth errors
4. **Token Storage**: Use memory + sessionStorage for better security
5. **Logout Timer**: Consider auto-logout after inactivity

## 🎯 Next Steps

1. ✅ Set up your frontend project
2. ✅ Install required dependencies (axios/fetch)
3. ✅ Copy authentication code from examples
4. ✅ Update API base URL
5. ✅ Test login flow
6. ✅ Implement auto-refresh interceptor
7. ✅ Add logout functionality
8. ✅ Test with your backend

Need help? Check the full integration guides in the `FrontendIntegration` folder!
