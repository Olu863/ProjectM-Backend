using System;

namespace ProjectM.Services.DTOs
{
    public class RepairDto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public int? TechnicianId { get; set; }
        public string TechnicianName { get; set; }
        public string EquipmentName { get; set; }
        public string EquipmentSerial { get; set; }
        public string IssueDescription { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public string PFIUrl { get; set; }
        public decimal? CostEstimate { get; set; }
        public decimal? ActualCost { get; set; }
        public int? DowntimeProjection { get; set; }
        public int? ActualDowntime { get; set; }
        public DateTime? EstimatedCompletionDate { get; set; }
        public DateTime? ActualCompletionDate { get; set; }
        public string Notes { get; set; }
        public int CreatedBy { get; set; }
        public string CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
