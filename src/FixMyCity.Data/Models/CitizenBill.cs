using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FixMyCity.Data.Models
{
    public class CitizenBill
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public int ProviderId { get; set; }
        public MunicipalProvider? Provider { get; set; }

        [Required]
        [MaxLength(100)]
        public string ConsumerNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string BillNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string BillingPeriod { get; set; } = string.Empty; // e.g. September 2026

        [MaxLength(50)]
        public string BillingPeriodBn { get; set; } = string.Empty; // e.g. September 2026

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LateFee { get; set; } = 0;

        public DateTime DueDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Unpaid"; // Unpaid, Paid, Overdue

        public List<PaymentTransaction> Transactions { get; set; } = new();
    }
}
