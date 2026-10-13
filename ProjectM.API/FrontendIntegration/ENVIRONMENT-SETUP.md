# Environment Setup Guide

## Backend Configuration (ProjectM.API)

### 1. appsettings.json

Update `ProjectM.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
	"DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ProjectM;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Jwt": {
	"Key": "ThisIsAVerySecureSecretKeyThatIsAtLeast32CharactersLong!",
	"Issuer": "ProjectM",
	"Audience": "ProjectMClient",
	"ExpiryInMinutes": 15,
	"RefreshTokenExpiryInDays": 7
  },
  "Logging": {
	"LogLevel": {
	  "Default": "Information",
	  "Microsoft.AspNetCore": "Warning"
	}
  },
  "AllowedHosts": "*"
}
```

### 2. appsettings.Development.json

Create/update `ProjectM.API/appsettings.Development.json`:

```json
{
  "Logging": {
	"LogLevel": {
	  "Default": "Debug",
	  "Microsoft.AspNetCore": "Information",
	  "Microsoft.EntityFrameworkCore": "Information"
	}
  },
  "Jwt": {
	"Key": "DevSecretKeyForTestingOnly32CharsMin!",
	"ExpiryInMinutes": 60
  }
}
```

### 3. User Secrets (Recommended for Development)

Instead of storing sensitive data in appsettings.json, use User Secrets:

```bash
cd ProjectM.API
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "YourSuperSecretKeyHere"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YourConnectionString"
```

### 4. CORS Configuration

Update `ProjectM.API/Program.cs` line 24-29 based on your environment:

**Development:**
```csharp
policy.WithOrigins(
	"http://localhost:3000",
	"http://localhost:4200",
	"http://localhost:5173",
	"http://localhost:8080"
)
```

**Production:**
```csharp
policy.WithOrigins(
	"https://yourdomain.com",
	"https://www.yourdomain.com",
	"https://app.yourdomain.com"
)
```

## Frontend Configuration

### React / Vite

Create `.env.development`:
```env
VITE_API_BASE_URL=https://localhost:7xxx/api
VITE_API_TIMEOUT=30000
```

Create `.env.production`:
```env
VITE_API_BASE_URL=https://api.yourdomain.com/api
VITE_API_TIMEOUT=30000
```

Usage in code:
```javascript
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;
```

### Create React App

Create `.env.development`:
```env
REACT_APP_API_BASE_URL=https://localhost:7xxx/api
REACT_APP_API_TIMEOUT=30000
```

Create `.env.production`:
```env
REACT_APP_API_BASE_URL=https://api.yourdomain.com/api
REACT_APP_API_TIMEOUT=30000
```

Usage in code:
```javascript
const API_BASE_URL = process.env.REACT_APP_API_BASE_URL;
```

### Next.js

Create `.env.local`:
```env
NEXT_PUBLIC_API_BASE_URL=https://localhost:7xxx/api
API_SECRET_KEY=your-secret-key
```

Create `.env.production`:
```env
NEXT_PUBLIC_API_BASE_URL=https://api.yourdomain.com/api
API_SECRET_KEY=your-production-secret
```

Usage in code:
```javascript
// Client-side
const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL;

// Server-side
const SECRET = process.env.API_SECRET_KEY;
```

### Angular

Create `src/environments/environment.ts`:
```typescript
export const environment = {
  production: false,
  apiBaseUrl: 'https://localhost:7xxx/api',
  apiTimeout: 30000
};
```

Create `src/environments/environment.prod.ts`:
```typescript
export const environment = {
  production: true,
  apiBaseUrl: 'https://api.yourdomain.com/api',
  apiTimeout: 30000
};
```

### Vue

Create `.env.development`:
```env
VUE_APP_API_BASE_URL=https://localhost:7xxx/api
VUE_APP_API_TIMEOUT=30000
```

Create `.env.production`:
```env
VUE_APP_API_BASE_URL=https://api.yourdomain.com/api
VUE_APP_API_TIMEOUT=30000
```

## Database Setup

### Apply Migrations

```bash
cd ProjectM.Infrastructure
dotnet ef database update --startup-project ../ProjectM.API
```

### Seed Initial Data (Optional)

Create an admin user manually or through seeding:

```sql
INSERT INTO Users (Name, Email, PasswordHash, Role, Company, Phone, IsActive, CreatedAt)
VALUES (
  'Admin User',
  'admin@example.com',
  '$2a$11$...',  -- Use BCrypt to hash password
  'Admin',
  'ProjectM',
  '1234567890',
  1,
  GETUTCDATE()
);
```

Or use EF Core seeding in `DbInitializer.cs`.

## SSL/TLS Configuration

### Development (Self-Signed Certificate)

```bash
dotnet dev-certs https --trust
```

### Production

- Use Let's Encrypt for free SSL certificates
- Configure certificate in hosting environment (Azure, AWS, etc.)
- Update appsettings.json with certificate path if needed

## Environment-Specific Settings Summary

| Setting | Development | Production |
|---------|-------------|------------|
| JWT Key | 32+ chars dev key | Strong random key |
| JWT Expiry | 60 minutes | 15 minutes |
| Refresh Token Expiry | 30 days | 7 days |
| CORS Origins | localhost:* | yourdomain.com |
| HTTPS Required | Optional | Required |
| Cookie Secure Flag | false | true |
| Logging Level | Debug | Warning |

## Security Checklist

- [ ] JWT secret key is at least 32 characters
- [ ] JWT secret is NOT committed to source control
- [ ] Connection string uses environment variable in production
- [ ] CORS is configured to specific origins (not *)
- [ ] HTTPS is enforced in production
- [ ] Cookie Secure flag is true in production
- [ ] SameSite is set to Strict or Lax
- [ ] Database connection uses least-privilege account
- [ ] Secrets are stored in Azure Key Vault / AWS Secrets Manager (production)

## Testing Configuration

### Postman Environment

Create environment in Postman with these variables:

```json
{
  "baseUrl": "https://localhost:7xxx/api",
  "accessToken": "",
  "refreshToken": "",
  "userId": ""
}
```

### cURL Testing

```bash
# Login
curl -X POST https://localhost:7xxx/api/users/login-with-cookie \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"yourpassword"}' \
  -c cookies.txt

# Use token
curl -X GET https://localhost:7xxx/api/users/1 \
  -H "Authorization: Bearer YOUR_TOKEN_HERE" \
  -b cookies.txt
```

## Deployment

### Azure App Service

Set Application Settings:
```
Jwt__Key = your-production-key
ConnectionStrings__DefaultConnection = your-connection-string
```

### Docker

Create `docker-compose.yml`:
```yaml
version: '3.8'
services:
  api:
	build: .
	ports:
	  - "5000:80"
	environment:
	  - Jwt__Key=${JWT_KEY}
	  - ConnectionStrings__DefaultConnection=${DB_CONNECTION}
```

Create `.env` file:
```env
JWT_KEY=your-secret-key
DB_CONNECTION=your-connection-string
```

## Troubleshooting

### Issue: JWT Key Too Short
**Error**: "IDX10603: The algorithm 'HS256' requires the SecurityKey.KeySize to be greater than '128' bits"
**Solution**: Use a key with at least 32 characters

### Issue: CORS Error
**Error**: "No 'Access-Control-Allow-Origin' header"
**Solution**: Add frontend URL to CORS policy in Program.cs

### Issue: Database Connection Failed
**Error**: "Cannot open database"
**Solution**: 
1. Check connection string
2. Verify SQL Server is running
3. Run migrations: `dotnet ef database update`

### Issue: Cookie Not Set
**Error**: Refresh token cookie not appearing
**Solution**: 
1. Set `Secure: false` in development (HTTP)
2. Ensure `credentials: 'include'` on frontend
3. Check CORS allows credentials
