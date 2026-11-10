using Microsoft.EntityFrameworkCore;
using ProjectM.Core.Entities;
using BCrypt.Net;

namespace ProjectM.Infrastructure.Data
{
    public static class DbInitializer
    {
        /// <summary>
        /// Seeds the first administrator and default settings into an EMPTY database.
        /// The password is supplied by the caller (from user-secrets / environment variables) -
        /// there is deliberately no built-in default password.
        /// </summary>
        public static async Task InitializeAsync(ApplicationDbContext context, string adminPassword, string? adminEmail = null)
        {
            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                return;
            }

            // Check if we already have data
            if (await context.Users.AnyAsync())
            {
                return; // DB has been seeded
            }

            // Seed the first admin user
            var adminUser = new User
            {
                Name = "System Administrator",
                Email = string.IsNullOrWhiteSpace(adminEmail) ? "admin@projectm.local" : adminEmail.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Phone = "",
                Company = "ProjectM",
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();

            // Seed default system settings
            var defaultSettings = new List<SystemSetting>
            {
                new SystemSetting
                {
                    SettingKey = "SLA_WARNING_THRESHOLD_HOURS",
                    SettingValue = "24",
                    Description = "Hours before SLA breach to send warning notifications",
                    CreatedAt = DateTime.UtcNow
                },
                new SystemSetting
                {
                    SettingKey = "DEFAULT_SLA_RESPONSE_TIME_HOURS",
                    SettingValue = "48",
                    Description = "Default SLA response time in hours",
                    CreatedAt = DateTime.UtcNow
                },
                new SystemSetting
                {
                    SettingKey = "EMAIL_NOTIFICATIONS_ENABLED",
                    SettingValue = "true",
                    Description = "Enable or disable email notifications",
                    CreatedAt = DateTime.UtcNow
                },
                new SystemSetting
                {
                    SettingKey = "MAX_FILE_UPLOAD_SIZE_MB",
                    SettingValue = "10",
                    Description = "Maximum file upload size in megabytes",
                    CreatedAt = DateTime.UtcNow
                }
            };

            context.SystemSettings.AddRange(defaultSettings);
            await context.SaveChangesAsync();
        }
    }
}
