using System;

namespace ProjectM.Core.Entities
{
    public class SLAHistory
    {
        public int Id { get; set; }
        public int SLAId { get; set; }
        public string Action { get; set; }
        public int PerformedBy { get; set; }
        public string Notes { get; set; }
        public string OldStatus { get; set; }
        public string NewStatus { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual SLA SLA { get; set; }
        public virtual User Performer { get; set; }
    }
}
