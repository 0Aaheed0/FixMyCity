using System;
using System.Threading.Tasks;

namespace FixMyCity.Services.Payments
{
    public class PaymentProcessRequest
    {
        public int BillId { get; set; }
        public string ConsumerNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "bKash";
        public string? CustomerPhone { get; set; }
        public string? CardNumber { get; set; }
        public string? CardExpiry { get; set; }
        public string? CardCvv { get; set; }
        public string? PinOrOtp { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class PaymentProcessResult
    {
        public bool IsSuccess { get; set; }
        public string TransactionReference { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string MessageBn { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public DateTime PaidAt { get; set; } = DateTime.UtcNow;
        public string? GatewayResponse { get; set; }
    }

    public interface IPaymentGateway
    {
        string GatewayName { get; }
        Task<PaymentProcessResult> ProcessPaymentAsync(PaymentProcessRequest request);
    }
}
