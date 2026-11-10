using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ProjectM.Infrastructure.Data;
using ProjectM.Services.Services;
using Microsoft.OpenApi.Models;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// 1. CONTROLLERS
// ========================================

builder.Services.AddControllers();


// ========================================
// 2. FLUENT VALIDATION
// ========================================

builder.Services.AddValidatorsFromAssemblyContaining<
    ProjectM.Services.Validators.CreateUserDtoValidator>();


// ========================================
// 3. CORS CONFIGURATION
// ========================================

var allowFrontendOrigins = "_allowFrontendOrigins";

var configuredOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>();

var allowedOrigins = (configuredOrigins != null &&
                      configuredOrigins.Length > 0)
    ? configuredOrigins
    : new[]
    {
        "http://localhost:5177",
        "http://localhost:3000",
        "http://localhost:4200",
        "http://localhost:5173"
    };

builder.Services.AddCors(options =>
{
    options.AddPolicy(allowFrontendOrigins, policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});


// ========================================
// 4. COOKIE POLICY
// ========================================

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Strict;

    options.HttpOnly = HttpOnlyPolicy.Always;

    options.Secure = CookieSecurePolicy.Always;
});


// ========================================
// 5. DATABASE CONFIGURATION
// ========================================

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("DefaultConnection is missing from configuration.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));


// ========================================
// 6. JWT CONFIGURATION
// ========================================

// Retrieve JWT settings from configuration.
// Keep the signing key outside source control.

var jwtKey = builder.Configuration["Jwt:Key"];

var jwtIssuer = builder.Configuration["Jwt:Issuer"];

var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrWhiteSpace(jwtKey) ||
    Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    Console.Error.WriteLine(
        "Jwt:Key is missing or too short. A minimum of 32 bytes is required. Configure it using User Secrets or an environment variable.");
}

if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    Console.Error.WriteLine("Jwt:Issuer is missing from configuration.");
}

if (string.IsNullOrWhiteSpace(jwtAudience))
{
    Console.Error.WriteLine("Jwt:Audience is missing from configuration.");
}


// ========================================
// 7. JWT AUTHENTICATION
// ========================================

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,

                ValidIssuer = jwtIssuer,

                ValidateAudience = true,

                ValidAudience = jwtAudience,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,

                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey ?? string.Empty)
                ),

                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });


// ========================================
// 8. AUTHORIZATION
// ========================================

builder.Services.AddAuthorization();


// ========================================
// 9. RATE LIMITING
// ========================================

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",

            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,

                Window = TimeSpan.FromMinutes(1),

                QueueLimit = 0
            }));
});


// ========================================
// 10. REGISTER APPLICATION SERVICES
// ========================================

builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<
    ProjectM.Services.Interfaces.IRepairService,
    ProjectM.Services.Services.RepairService>();

builder.Services.AddScoped<
    ProjectM.Services.Interfaces.ISLAService,
    ProjectM.Services.Services.SLAService>();

builder.Services.AddScoped<
    ProjectM.Services.Interfaces.IClientCompanyService,
    ProjectM.Services.Services.ClientCompanyService>();

builder.Services.AddScoped<
    ProjectM.Services.Interfaces.INotificationService,
    ProjectM.Services.Services.NotificationService>();

builder.Services.AddScoped<
    ProjectM.Services.Interfaces.IPFIService,
    ProjectM.Services.Services.PFIService>();


// ========================================
// 11. BACKGROUND SERVICES
// ========================================

builder.Services.AddHostedService<
    ProjectM.API.Services.TokenCleanupService>();


// ========================================
// 12. SWAGGER CONFIGURATION
// ========================================

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ProjectM API",

        Version = "v1",

        Description = "SLA Sentinel API Documentation"
    });

    // JWT Bearer Authentication

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description =
            "Enter your JWT token. Use the Bearer scheme.",

        Name = "Authorization",

        In = ParameterLocation.Header,

        Type = SecuritySchemeType.Http,

        Scheme = "bearer",

        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,

                    Id = "Bearer"
                }
            },

            Array.Empty<string>()
        }
    });
});


// ========================================
// 13. BUILD APPLICATION
// ========================================

var app = builder.Build();


// ========================================
// 14. SWAGGER MIDDLEWARE
// ========================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI();
}


// ========================================
// 15. HTTP REQUEST PIPELINE
// ========================================

app.UseHttpsRedirection();

app.UseCors(allowFrontendOrigins);

app.UseCookiePolicy();

app.UseRateLimiter();

// JWT authentication must run before authorization.
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();


// ========================================
// 16. DATABASE CONNECTION VERIFICATION
// ========================================

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    var logger = scope.ServiceProvider
        .GetRequiredService<ILogger<Program>>();

    var canConnect = await context.Database.CanConnectAsync();

        if (canConnect)
        {
            logger.LogInformation(
                "Database connection verified successfully.");

            logger.LogInformation(
                "Connected to database: {Database}",
                context.Database.GetDbConnection().Database);

            // Check pending migrations.

            var pendingMigrations =
                await context.Database.GetPendingMigrationsAsync();

            if (!pendingMigrations.Any())
            {
                logger.LogInformation(
                    "Database schema is synchronized with entity models.");
            }
            else
            {
                logger.LogWarning(
                    "{Count} pending migration(s) detected.",
                    pendingMigrations.Count());

                logger.LogInformation(
                    "Run: dotnet ef database update " +
                    "--project ProjectM.Infrastructure " +
                    "--startup-project ProjectM.API");
            }
        }
        else
        {
            logger.LogError(
                "Cannot connect to database. Check your connection string.");
        }
    // ========================================
    // 17. INITIAL ADMIN ACCOUNT SEEDING
    // ========================================

    var seedAdminPassword =
        app.Configuration["Seed:AdminPassword"];

    if (!string.IsNullOrWhiteSpace(seedAdminPassword))
    {
        await DbInitializer.InitializeAsync(
            context,
            seedAdminPassword,
            app.Configuration["Seed:AdminEmail"]);

        logger.LogInformation(
            "Initial Admin seeding completed.");
    }
}


// ========================================
// 18. RUN APPLICATION
// ========================================

app.Run();