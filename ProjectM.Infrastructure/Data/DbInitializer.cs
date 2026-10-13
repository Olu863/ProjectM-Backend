using Microsoft.EntityFrameworkCore;
using ProjectM.Core.Entities;
using BCrypt.Net;

namespace ProjectM.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(ApplicationDbContext context)
        {
            // Check if we already have data
            if (await context.Users.AnyAsync())
            {
                return; // DB has been seeded
            }

            // Seed default admin user
            var adminUser = new User
            {
                Name = "System Administrator",
                Email = "admin@projectm.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Phone = "+1234567890",
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
