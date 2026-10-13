using System;

namespace ProjectM.Core.Entities
{
    public class ClientCompany
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string CompanyName { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? TaxID { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; } = "Nigeria";
        public string? PostalCode { get; set; }
        public string? Website { get; set; }
        public string? Industry { get; set; }
        public int? EmployeeCount { get; set; }
        public DateTime? ContractStartDate { get; set; }
        public DateTime? ContractEndDate { get; set; }
        public string? PaymentTerms { get; set; }
        public string? PreferredCurrency { get; set; } = "NGN";
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual User User { get; set; }
    }
}