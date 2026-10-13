# Environment Variables Setup

This project uses environment variables for sensitive configuration to keep data secure.

## Local Development

Configuration is set up in the `launchSettings.json` file for local development. When you run the project from Visual Studio, these environment variables will be automatically loaded.

### Environment Variables:

#### JWT Configuration:
- `Jwt__Key` - The secret key used to sign JWT tokens
- `Jwt__Issuer` - The issuer of the JWT token (ProjectM)
- `Jwt__Audience` - The intended audience for the JWT token (ProjectMClient)
- `Jwt__ExpiryInHours` - Token expiration time in hours (24)

#### Database Configuration:
- `ConnectionStrings__DefaultConnection` - The SQL Server connection string with credentials

## Production Deployment

### Azure App Service:
1. Go to your App Service in Azure Portal
2. Navigate to **Configuration** > **Application settings**
3. Add the following settings:

**JWT Settings:**
   - Name: `Jwt__Key`, Value: `<your-production-jwt-key>`
   - Name: `Jwt__Issuer`, Value: `ProjectM`
   - Name: `Jwt__Audience`, Value: `ProjectMClient`
   - Name: `Jwt__ExpiryInHours`, Value: `24`

**Database Settings:**
   - Name: `ConnectionStrings__DefaultConnection`, Value: `<your-production-connection-string>`

4. Click **Save**

### Docker / Other Hosting:
Set environment variables in your deployment configuration:

```bash
# JWT Configuration
Jwt__Key=<your-production-jwt-key>
Jwt__Issuer=ProjectM
Jwt__Audience=ProjectMClient
Jwt__ExpiryInHours=24

# Database Configuration
ConnectionStrings__DefaultConnection=<your-production-connection-string>
```

## Generating a New JWT Key

To generate a new secure JWT key, run this PowerShell command:

```powershell
$bytes = New-Object byte[] 64
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
[Convert]::ToBase64String($bytes)
```

**Important:** 
- Never commit the actual JWT key to source control
- Use different keys for development and production
- Keep your production keys secure and rotate them periodically
