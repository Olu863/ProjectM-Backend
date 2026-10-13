using System;

namespace ProjectM.Core.Entities
{
    public class SLAReminder
    {
        public int Id { get; set; }
        public int SLAId { get; set; }
        public string ReminderType { get; set; }
        public DateTime ScheduledDate { get; set; }
        public DateTime? SentDate { get; set; }
        public int SentTo { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual SLA SLA { get; set; }
        public virtual User Recipient { get; set; }
    }
}
