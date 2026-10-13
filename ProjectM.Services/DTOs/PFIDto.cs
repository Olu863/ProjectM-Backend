using System;
using System.Collections.Generic;

namespace ProjectM.Services.DTOs
{
    public class PFIDto
    {
        public int Id { get; set; }
        public int RepairId { get; set; }
        public string PFINumber { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public int? ApprovedBy { get; set; }
        public string ApprovedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<PFILineItemDto> LineItems { get; set; }
    }

    public class PFILineItemDto
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class CreatePFIDto
    {
        public int RepairId { get; set; }
        public List<CreatePFILineItemDto> LineItems { get; set; }
    }

    public class CreatePFILineItemDto
    {
        public string Description { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class UpdatePFIDto
    {
        public string Status { get; set; }
        public List<CreatePFILineItemDto> LineItems { get; set; }
    }
}
