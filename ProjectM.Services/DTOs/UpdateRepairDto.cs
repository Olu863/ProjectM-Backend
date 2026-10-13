using System;

namespace ProjectM.Services.DTOs
{
    public class UpdateRepairDto
    {
        public int? TechnicianId { get; set; }
        public string EquipmentName { get; set; }
        public string EquipmentSerial { get; set; }
        public string IssueDescription { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public decimal? CostEstimate { get; set; }
        public decimal? ActualCost { get; set; }
        public int? DowntimeProjection { get; set; }
        public int? ActualDowntime { get; set; }
        public DateTime? EstimatedCompletionDate { get; set; }
        public DateTime? ActualCompletionDate { get; set; }
        public string Notes { get; set; }
    }
}
