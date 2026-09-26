using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FixMyCity.Data.Models;

namespace FixMyCity.Web.Models
{
    public class GatewayDashboardViewModel
    {
        public List<MunicipalService> Services { get; set; } = new();
        public List<CitizenBill> PendingBills { get; set; } = new();
        public List<PaymentTransaction> RecentTransactions { get; set; } = new();
        public int TotalPaidCount { get; set; }
        public decimal TotalPaidAmount { get; set; }
    }

    public class BillLookupViewModel
    {
        [Required(ErrorMessage = "Please select a municipal service provider.")]
        public int ProviderId { get; set; }

        [Required(ErrorMessage = "Please enter your Account / Consumer Number.")]
        [Display(Name = "Account / Consumer Number")]
        public string ConsumerNumber { get; set; } = string.Empty;

        public List<MunicipalProvider> Providers { get; set; } = new();
        public CitizenBill? FoundBill { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class PayBillViewModel
    {
        public int BillId { get; set; }
        public CitizenBill? Bill { get; set; }

        [Required(ErrorMessage = "Please choose a payment method.")]
        public string PaymentMethod { get; set; } = "bKash";

        [Phone]
        [Display(Name = "MFS Wallet Number")]
        public string? CustomerPhone { get; set; }

        [Display(Name = "Card Number")]
        public string? CardNumber { get; set; }

        [Display(Name = "Expiry Date (MM/YY)")]
        public string? CardExpiry { get; set; }

        [Display(Name = "CVV")]
        public string? CardCvv { get; set; }

        [Display(Name = "Sandbox PIN")]
        public string? PinOrOtp { get; set; }
    }
}
