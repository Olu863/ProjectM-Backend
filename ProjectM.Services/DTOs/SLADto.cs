using System;

namespace ProjectM.Services.DTOs
{
    public class SLADto
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public string ClientName { get; set; }
        public string SLAType { get; set; }
        public decimal? ContractValue { get; set; }
        public int? Escalation1DurationInHours { get; set; }
        public int? Escalation2DurationInHours { get; set; }
        public int? Escalation3DurationInHours { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? RenewalDate { get; set; }
        public string Status { get; set; }
        public string Terms { get; set; }
        public int CreatedBy { get; set; }
        public string CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateSLADto
    {
        public int ClientId { get; set; }
        public string SLAType { get; set; }
        public decimal? ContractValue { get; set; }
        public int? Escalation1DurationInHours { get; set; }
        public int? Escalation2DurationInHours { get; set; }
        public int? Escalation3DurationInHours { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? RenewalDate { get; set; }
        public string Terms { get; set; }
    }

    public class UpdateSLADto
    {
        public string SLAType { get; set; }
        public decimal? ContractValue { get; set; }
        public int? Escalation1DurationInHours { get; set; }
        public int? Escalation2DurationInHours { get; set; }
        public int? Escalation3DurationInHours { get; set; }
        public DateTime? RenewalDate { get; set; }
        public string Status {get; set; }
        public string Terms { get; set; }
    }
}