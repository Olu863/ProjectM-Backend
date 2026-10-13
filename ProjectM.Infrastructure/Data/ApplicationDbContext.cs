using Microsoft.EntityFrameworkCore;
using ProjectM.Core.Entities;

namespace ProjectM.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<SLA> SLAs { get; set; }
        public DbSet<SLAHistory> SLAHistories { get; set; }
        public DbSet<Repair> Repairs { get; set; }
        public DbSet<PFI> PFIs { get; set; }
        public DbSet<PFILineItem> PFILineItems { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SLAReminder> SLAReminders { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }
        public DbSet<ClientCompany> ClientCompanies { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Role);

            // SLA configuration
            modelBuilder.Entity<SLA>()
                .HasIndex(s => s.ClientId);

            modelBuilder.Entity<SLA>()
                .HasIndex(s => s.Status);

            modelBuilder.Entity<SLA>()
                .HasIndex(s => s.RenewalDate);

            modelBuilder.Entity<SLA>()
                .HasIndex(s => new { s.ClientId, s.Status });

            modelBuilder.Entity<SLA>()
                .HasOne(s => s.Client)
                .WithMany(u => u.SLAs)
                .HasForeignKey(s => s.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SLA>()
                .HasOne(s => s.Creator)
                .WithMany(u => u.CreatedSLAs)
                .HasForeignKey(s => s.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // SLAHistory configuration
            modelBuilder.Entity<SLAHistory>()
                .HasIndex(h => h.SLAId);

            modelBuilder.Entity<SLAHistory>()
                .HasIndex(h => h.CreatedAt);

            // Repair configuration
            modelBuilder.Entity<Repair>()
                .HasIndex(r => r.ClientId);

            modelBuilder.Entity<Repair>()
                .HasIndex(r => r.TechnicianId);

            modelBuilder.Entity<Repair>()
                .HasIndex(r => r.Status);

            modelBuilder.Entity<Repair>()
                .HasIndex(r => r.CreatedAt);

            modelBuilder.Entity<Repair>()
                .HasOne(r => r.Client)
                .WithMany(u => u.ClientRepairs)
                .HasForeignKey(r => r.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Repair>()
                .HasOne(r => r.Technician)
                .WithMany(u => u.TechnicianRepairs)
                .HasForeignKey(r => r.TechnicianId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Repair>()
                .HasOne(r => r.Creator)
                .WithMany(u => u.CreatedRepairs)
                .HasForeignKey(r => r.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // PFI configuration
            modelBuilder.Entity<PFI>()
                .HasIndex(p => p.RepairId);

            modelBuilder.Entity<PFI>()
                .HasIndex(p => p.Status);

            modelBuilder.Entity<PFI>()
                .HasIndex(p => p.PFINumber)
                .IsUnique();

                        modelBuilder.Entity<PFI>()
                .HasOne(p => p.Repair)
                .WithMany(r => r.PFIs)
                .HasForeignKey(p => p.RepairId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PFI>()
                .HasOne(p => p.Approver)
                .WithMany(u => u.ApprovedPFIs)
                .HasForeignKey(p => p.ApprovedBy)
                .OnDelete(DeleteBehavior.Restrict);

            // PFILineItem configuration
            modelBuilder.Entity<PFILineItem>()
                .HasIndex(li => li.PFIId);

            // Notification configuration
            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.UserId);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.IsRead);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.CreatedAt);

            // AuditLog configuration
            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => new { a.EntityType, a.EntityId });

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.CreatedAt);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.PerformedBy);

            // SLAReminder configuration
            modelBuilder.Entity<SLAReminder>()
                .HasIndex(r => r.SLAId);

            modelBuilder.Entity<SLAReminder>()
                .HasIndex(r => r.Status);

            modelBuilder.Entity<SLAReminder>()
                .HasIndex(r => r.ScheduledDate);

            // SystemSetting configuration
            modelBuilder.Entity<SystemSetting>()
                .HasIndex(s => s.SettingKey)
                .IsUnique();

            // ActivityLog configuration
            modelBuilder.Entity<ActivityLog>()
                .HasIndex(a => a.UserId);

            modelBuilder.Entity<ActivityLog>()
                .HasIndex(a => a.CreatedAt);

            // ClientCompany configuration
            modelBuilder.Entity<ClientCompany>()
                .HasIndex(c => c.UserId)
                .IsUnique();

            modelBuilder.Entity<ClientCompany>()
                .HasIndex(c => c.State);

            // RefreshToken configuration
            modelBuilder.Entity<RefreshToken>()
                .HasIndex(rt => rt.UserId);

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(rt => rt.Token)
                .IsUnique();

            modelBuilder.Entity<RefreshToken>()
                .HasIndex(rt => rt.ExpiresAt);

            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
