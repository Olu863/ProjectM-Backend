using System;
using System.Collections.Generic;

namespace ProjectM.Core.Entities
{
    public class SLA
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string DocumentUrl { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime RenewalDate { get; set; }
        public string ServiceScope { get; set; }
        public decimal? ContractValue { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ExecutedAt { get; set; }
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public virtual User Client { get; set; }
        public virtual User Creator { get; set; }
        public virtual ICollection<SLAHistory> SLAHistories { get; set; }
        public virtual ICollection<SLAReminder> SLAReminders { get; set; }
    }
}
