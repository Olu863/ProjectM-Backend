using System;
using System.Collections.Generic;

namespace ProjectM.Core.Entities
{
    public class PFI
    {
        public int Id { get; set; }
        public int RepairId { get; set; }
        public string PFINumber { get; set; }
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public DateTime? ValidUntil { get; set; }
        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public string? Currency { get; set; } = "NGN";
        public decimal Tax { get; set; } = 0;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";
        public string? FileUrl { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual Repair Repair { get; set; }
        public virtual User Approver { get; set; }
        public virtual ICollection<PFILineItem> LineItems { get; set; }
    }
}