using System;
using System.Collections.Generic;

namespace ProjectM.Core.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; }
        public string? Company { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<SLA> SLAs { get; set; }
        public virtual ICollection<SLA> CreatedSLAs { get; set; }
        public virtual ICollection<SLAHistory> SLAHistories { get; set; }
        public virtual ICollection<Repair> ClientRepairs { get; set; }
        public virtual ICollection<Repair> TechnicianRepairs { get; set; }
        public virtual ICollection<Repair> CreatedRepairs { get; set; }
        public virtual ICollection<PFI> ApprovedPFIs { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual ICollection<AuditLog> AuditLogs { get; set; }
        public virtual ICollection<SLAReminder> SLAReminders { get; set; }
        public virtual ICollection<ActivityLog> ActivityLogs { get; set; }
        public virtual ClientCompany ClientCompany { get; set; }
    }
}