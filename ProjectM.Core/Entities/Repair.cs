using System;
using System.Collections.Generic;

namespace ProjectM.Core.Entities
{
    public class Repair
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public int? TechnicianId { get; set; }
        public string EquipmentName { get; set; }
        public string? EquipmentSerial { get; set; }
        public string IssueDescription { get; set; }
        public string Priority { get; set; } = "Medium";
        public string Status { get; set; } = "Requested";
        public string? PFIUrl { get; set; }
        public decimal? CostEstimate { get; set; }
        public decimal? ActualCost { get; set; }
        public int? DowntimeProjection { get; set; }
        public int? ActualDowntime { get; set; }
        public DateTime? EstimatedCompletionDate { get; set; }
        public DateTime? ActualCompletionDate { get; set; }
        public string? Notes { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual User Client { get; set; }
        public virtual User Technician { get; set; }
        public virtual User Creator { get; set; }
        public virtual ICollection<PFI> PFIs { get; set; }
    }
}