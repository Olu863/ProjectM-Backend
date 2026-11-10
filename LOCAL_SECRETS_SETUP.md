# Local secrets setup

Secrets (database password, JWT signing key, first-admin password) must **never** be committed.
The API reads them from .NET user-secrets locally and from environment variables in production.

## One-time local setup

Run these from the repository root (the API project already has a UserSecretsId):

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=...;User Id=...;Password=...;TrustServerCertificate=True;" --project ProjectM.API
# Generate a long random key, e.g. in PowerShell:
#   [Convert]::ToBase64String((1..64 | % { Get-Random -Maximum 256 }) -as [byte[]])
dotnet user-secrets set "Jwt:Key" "<random 64+ character string>" --project ProjectM.API
```

`dotnet ef migrations ...` / `dotnet ef database update` read the same secrets, so run them with
`--project ProjectM.Infrastructure --startup-project ProjectM.API` as before.

The API refuses to start if `Jwt:Key` is missing or shorter than 32 characters.

## First administrator

Public registration can no longer create `Admin` accounts. On an empty database, set a password
(user-secrets or the `Seed__AdminPassword` environment variable), start the API once, then remove it:

```powershell
dotnet user-secrets set "Seed:AdminPassword" "<a strong password>" --project ProjectM.API
dotnet user-secrets set "Seed:AdminEmail" "you@yourcompany.com" --project ProjectM.API   # optional
```

Staff who register (Manager / Technician / Operations) are created inactive. An Admin finds them with
`GET /api/Users?isActive=false` and approves each with `POST /api/Users/{id}/activate`.

## Production

Set environment variables instead: `ConnectionStrings__DefaultConnection`, `Jwt__Key`,
`Cors__AllowedOrigins__0=https://your-site.netlify.app` (add `__1`, `__2` ... for more origins).
