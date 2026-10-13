using System;

namespace ProjectM.Services.DTOs
{
    public class CreateRepairDto
    {
        public int ClientId { get; set; }
        public string EquipmentName { get; set; }
        public string EquipmentSerial { get; set; }
        public string IssueDescription { get; set; }
        public string Priority { get; set; } = "Normal";
        public decimal? CostEstimate { get; set; }
        public int? DowntimeProjection { get; set; }
        public DateTime? EstimatedCompletionDate { get; set; }
        public string Notes { get; set; }
    }
}
