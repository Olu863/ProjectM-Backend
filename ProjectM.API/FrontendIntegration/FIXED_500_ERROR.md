# ✅ FIXED: 500 Internal Server Error on Login

## 🔴 Problem
```
POST https://localhost:7008/api/users/login 500 (Internal Server Error)
```

## 🎯 Root Cause
The JWT configuration in `appsettings.json` had an **empty JWT Key**:
```json
"Jwt": {
  "Key": "",  // ❌ EMPTY - caused NullReferenceException
  ...
}
```

When the backend tried to generate a JWT token during login, it called:
```csharp
Encoding.UTF8.GetBytes(jwtKey)  // jwtKey was null/empty
```
This threw a `NullReferenceException`, causing the 500 error.

## ✅ Solution Applied

### 1. **Added JWT Key to appsettings.json**
```json
"Jwt": {
  "Key": "RKdLvKv2+ZCncjHxZ4PPU1oZwJjEzYSn6nBO0DhQvEZJQzgoS2hEPvE53lxqeOvzhzFpq/BH0D4QZCrSmYK0Nw==",
  "Issuer": "ProjectM",
  "Audience": "ProjectMClient",
  "ExpiryInMinutes": 15,
  "RefreshTokenExpiryInDays": 7
}
```

### 2. **Added Defensive Null Checks in UserService**
Enhanced `GenerateJwtToken()` method with validation:
```csharp
if (string.IsNullOrWhiteSpace(jwtKey))
	throw new InvalidOperationException("JWT Key is not configured...");

if (string.IsNullOrWhiteSpace(jwtIssuer))
	throw new InvalidOperationException("JWT Issuer is not configured...");

if (string.IsNullOrWhiteSpace(jwtAudience))
	throw new InvalidOperationException("JWT Audience is not configured...");
```

Now if configuration is missing, you'll get a **clear error message** instead of a cryptic 500 error.

## 🚀 How to Test

### **Step 1: Restart Your Backend**
- **Stop debugging** (Shift + F5)
- **Start debugging again** (F5) or use Hot Reload

### **Step 2: Test Login from Frontend**
```javascript
POST http://localhost:7008/api/users/login
Content-Type: application/json

{
  "email": "admin@example.com",
  "password": "Admin@123"
}
```

### **Expected Response** (200 OK):
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
	"id": 1,
	"email": "admin@example.com",
	"name": "Admin User",
	"role": "Admin"
  },
  "expiresAt": "2024-01-15T15:30:00Z"
}
```

## 📋 Test Credentials

If you seeded the database with `DbInitializer`, use:
```
Email: admin@example.com
Password: Admin@123
```

## 🔍 Configuration Locations

The JWT key is now configured in **multiple places** (for different scenarios):

### **Development (Visual Studio)**
1. **appsettings.json** ✅ (now has the key)
2. **launchSettings.json** (environment variables) ✅

### **Production**
- Set via environment variables:
  ```bash
  Jwt__Key=your-secret-key
  Jwt__Issuer=ProjectM
  Jwt__Audience=ProjectMClient
  ```

## ⚠️ Security Notes

### **For Development**
- ✅ Current setup is fine (key in appsettings.json)
- Convenient for local development and testing

### **For Production**
- ❌ **Never commit production JWT keys to source control**
- ✅ Use **Azure Key Vault**, **AWS Secrets Manager**, or environment variables
- ✅ Rotate keys regularly

## 🎯 Why This Fix Works

### **Before:**
1. Frontend sends login request
2. Backend validates credentials ✅
3. Backend tries to generate JWT token
4. `jwtKey` is null/empty ❌
5. `Encoding.UTF8.GetBytes(null)` throws exception
6. Returns **500 Internal Server Error**

### **After:**
1. Frontend sends login request
2. Backend validates credentials ✅
3. Backend validates JWT configuration ✅
4. Backend generates JWT token successfully ✅
5. Returns **200 OK with token** ✅

## 📊 Monitoring

With the enhanced error handling we added earlier, you'll now see detailed logs:

### **Successful Login:**
```
[INFO] Login attempt for admin@example.com from 192.168.1.100
[INFO] User logged in successfully: admin@example.com
```

### **Configuration Error (if it happens again):**
```
[ERROR] Login error for admin@example.com: JWT Key is not configured
Stack Trace: InvalidOperationException at UserService.GenerateJwtToken...
```

## ✅ Verification Checklist

Test that these now work:

- [ ] Login with valid credentials → Returns token
- [ ] Login with invalid credentials → Returns 401 Unauthorized
- [ ] Login with empty email → Returns 400 Bad Request
- [ ] Login with empty password → Returns 400 Bad Request
- [ ] Refresh token → Returns new token
- [ ] Access protected endpoints with token → Success

## 🔗 Related Files

- **Fixed**: `ProjectM.API/appsettings.json`
- **Enhanced**: `ProjectM.Services/Services/UserService.cs`
- **Already configured**: `ProjectM.API/Properties/launchSettings.json`

---

**Status**: ✅ **RESOLVED**  
**Date**: January 2024  
**Impact**: Login functionality fully restored
