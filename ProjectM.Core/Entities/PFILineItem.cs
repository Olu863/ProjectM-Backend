using System;

namespace ProjectM.Core.Entities
{
    public class PFILineItem
    {
        public int Id { get; set; }
        public int PFIId { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual PFI PFI { get; set; }
    }
}
