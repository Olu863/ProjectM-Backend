using System;

namespace ProjectM.Core.Entities
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string EntityType { get; set; }
        public int EntityId { get; set; }
        public string Action { get; set; }
        public int PerformedBy { get; set; }
        public string OldValues { get; set; }
        public string NewValues { get; set; }
        public string IPAddress { get; set; }
        public string UserAgent { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User Performer { get; set; }
    }
}
