using System;

namespace ProjectM.Core.Entities
{
    public class ActivityLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ActivityType { get; set; }
        public string Description { get; set; }
        public string IPAddress { get; set; }
        public string UserAgent { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User User { get; set; }
    }
}
