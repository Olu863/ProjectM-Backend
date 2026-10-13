using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ProjectM.Infrastructure.Data;
using ProjectM.Services.Services;
using Microsoft.OpenApi.Models;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add FluentValidation validators
builder.Services.AddValidatorsFromAssemblyContaining<ProjectM.Services.Validators.CreateUserDtoValidator>();

// ========================================
// CORS Configuration
// ========================================
// 1. Define CORS policy name
var allowFrontendOrigins = "_allowFrontendOrigins";

// 2. Configure CORS to allow frontend connections with JWT + Cookies
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: allowFrontendOrigins,
        policy =>
        {
            policy.WithOrigins(
                    "http://localhost:5177",  // Your frontend (primary)
                    "http://localhost:3000",  // React default
                    "http://localhost:4200",  // Angular default
                    "http://localhost:5173"   // Vite default
                )
                .AllowAnyMethod()           // Allows GET, POST, PUT, DELETE, etc.
                .AllowAnyHeader()           // Allows Authorization, Content-Type, etc.
                .AllowCredentials();        // REQUIRED: Enables cookies for refresh tokens
        });
});

// Configure cookie policy for secure refresh token storage
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Strict;
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.Always; // Requires HTTPS
});

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

builder.Services.AddAuthorization();

// Register Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ProjectM.Services.Interfaces.IRepairService, ProjectM.Services.Services.RepairService>();
builder.Services.AddScoped<ProjectM.Services.Interfaces.ISLAService, ProjectM.Services.Services.SLAService>();
builder.Services.AddScoped<ProjectM.Services.Interfaces.IClientCompanyService, ProjectM.Services.Services.ClientCompanyService>();
builder.Services.AddScoped<ProjectM.Services.Interfaces.INotificationService, ProjectM.Services.Services.NotificationService>();
builder.Services.AddScoped<ProjectM.Services.Interfaces.IPFIService, ProjectM.Services.Services.PFIService>();

// Register Background Services
builder.Services.AddHostedService<ProjectM.API.Services.TokenCleanupService>();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ProjectM API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

var app = builder.Build();

// ========================================
// HTTP Request Pipeline Configuration
// ========================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware order is critical for security and functionality:
app.UseHttpsRedirection();              // 1. Redirect HTTP to HTTPS
app.UseCors(allowFrontendOrigins);      // 2. Enable CORS (before auth)
app.UseCookiePolicy();                  // 3. Apply cookie security policies
app.UseAuthentication();                // 4. Validate JWT tokens
app.UseAuthorization();                 // 5. Check user permissions
app.MapControllers();                   // 6. Route to controllers

// Verify database connection on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // This will verify the database exists and connection works
        var canConnect = await context.Database.CanConnectAsync();

        if (canConnect)
        {
            logger.LogInformation("✅ Database connection verified successfully");
            logger.LogInformation("💾 Connected to database: ProjectM.db");

            // Check if schema is up to date (without applying migrations)
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (!pendingMigrations.Any())
            {
                logger.LogInformation("✅ Database schema is synchronized with entity models");
            }
            else
            {
                logger.LogWarning("⚠️  {Count} pending migration(s) detected. Schema may need updates.", pendingMigrations.Count());
                logger.LogInformation("    Run manually: dotnet ef database update --project ProjectM.Infrastructure --startup-project ProjectM.API");
            }
        }
        else
        {
            logger.LogError("❌ Cannot connect to database. Please check connection string.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Database verification failed: {Message}", ex.Message);
    }

    // Optional: Seed default data if database is empty
    // Uncomment if you want to add default admin user and settings
    /*
    await DbInitializer.InitializeAsync(context);
    */
}

app.Run();
