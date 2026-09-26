using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FixMyCity.Data.Models
{
    public class PaymentTransaction
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string TransactionReference { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public int? BillId { get; set; }
        public CitizenBill? Bill { get; set; }

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "bKash"; // bKash, Nagad, Rocket, Upay, Card, InternetBanking, Mock

        [MaxLength(50)]
        public string? PaymentMethodBn { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Fee { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Success"; // Success, Failed, Pending

        [MaxLength(50)]
        public string? CustomerPhone { get; set; }

        public string? GatewayResponse { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }
    }
}
