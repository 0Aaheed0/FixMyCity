using System;
using System.Threading.Tasks;

namespace FixMyCity.Services.Payments
{
    public class BkashPaymentGateway : IPaymentGateway
    {
        public string GatewayName => "bKash";

        public async Task<PaymentProcessResult> ProcessPaymentAsync(PaymentProcessRequest request)
        {
            await Task.Delay(150); // Simulate network roundtrip

            // Validate phone number format for bKash (e.g. 01XXXXXXXXX)
            if (string.IsNullOrWhiteSpace(request.CustomerPhone) || request.CustomerPhone.Length < 11)
            {
                return new PaymentProcessResult
                {
                    IsSuccess = false,
                    TransactionReference = $"FAIL-BKASH-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                    Message = "Invalid bKash wallet number. Must be at least 11 digits.",
                    MessageBn = "Invalid bKash wallet number. Must be at least 11 digits.",
                    GatewayResponse = "REJECTED_INVALID_WALLET"
                };
            }

            var refId = $"TRX-BKASH-{DateTime.UtcNow:yyyyMMddHHmmss}-{new Random().Next(1000, 9999)}";
            return new PaymentProcessResult
            {
                IsSuccess = true,
                TransactionReference = refId,
                Message = $"bKash Sandbox Payment verified for BDT {request.Amount:N2}. Wallet: {request.CustomerPhone}",
                MessageBn = $"bKash Sandbox Payment verified for BDT {request.Amount:N2}. Wallet: {request.CustomerPhone}",
                Fee = Math.Round(request.Amount * 0.015m, 2), // 1.5% MFS gateway fee
                PaidAt = DateTime.UtcNow,
                GatewayResponse = "SUCCESS_SANDBOX_SETTLED"
            };
        }
    }

    public class NagadPaymentGateway : IPaymentGateway
    {
        public string GatewayName => "Nagad";

        public async Task<PaymentProcessResult> ProcessPaymentAsync(PaymentProcessRequest request)
        {
            await Task.Delay(150);

            if (string.IsNullOrWhiteSpace(request.CustomerPhone) || request.CustomerPhone.Length < 11)
            {
                return new PaymentProcessResult
                {
                    IsSuccess = false,
                    TransactionReference = $"FAIL-NAGAD-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                    Message = "Invalid Nagad account number. Must be at least 11 digits.",
                    MessageBn = "Invalid Nagad account number. Must be at least 11 digits.",
                    GatewayResponse = "REJECTED_INVALID_ACCOUNT"
                };
            }

            var refId = $"TRX-NAGAD-{DateTime.UtcNow:yyyyMMddHHmmss}-{new Random().Next(1000, 9999)}";
            return new PaymentProcessResult
            {
                IsSuccess = true,
                TransactionReference = refId,
                Message = $"Nagad Sandbox Payment verified for BDT {request.Amount:N2}. Account: {request.CustomerPhone}",
                MessageBn = $"Nagad Sandbox Payment verified for BDT {request.Amount:N2}. Account: {request.CustomerPhone}",
                Fee = Math.Round(request.Amount * 0.012m, 2), // 1.2%
                PaidAt = DateTime.UtcNow,
                GatewayResponse = "SUCCESS_SANDBOX_SETTLED"
            };
        }
    }

    public class RocketPaymentGateway : IPaymentGateway
    {
        public string GatewayName => "Rocket";

        public async Task<PaymentProcessResult> ProcessPaymentAsync(PaymentProcessRequest request)
        {
            await Task.Delay(150);

            if (string.IsNullOrWhiteSpace(request.CustomerPhone) || request.CustomerPhone.Length < 11)
            {
                return new PaymentProcessResult
                {
                    IsSuccess = false,
                    TransactionReference = $"FAIL-ROCKET-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                    Message = "Invalid Rocket wallet number.",
                    MessageBn = "Invalid Rocket wallet number.",
                    GatewayResponse = "REJECTED_INVALID_WALLET"
                };
            }

            var refId = $"TRX-ROCKET-{DateTime.UtcNow:yyyyMMddHHmmss}-{new Random().Next(1000, 9999)}";
            return new PaymentProcessResult
            {
                IsSuccess = true,
                TransactionReference = refId,
                Message = $"Rocket DBBL Sandbox Payment verified for BDT {request.Amount:N2}.",
                MessageBn = $"Rocket DBBL Sandbox Payment verified for BDT {request.Amount:N2}.",
                Fee = Math.Round(request.Amount * 0.01m, 2),
                PaidAt = DateTime.UtcNow,
                GatewayResponse = "SUCCESS_SANDBOX_SETTLED"
            };
        }
    }

    public class CardPaymentGateway : IPaymentGateway
    {
        public string GatewayName => "Card";

        public async Task<PaymentProcessResult> ProcessPaymentAsync(PaymentProcessRequest request)
        {
            await Task.Delay(200);

            // Basic check for card format
            var cardNum = request.CardNumber?.Replace(" ", "").Replace("-", "") ?? string.Empty;
            if (cardNum.Length < 12)
            {
                return new PaymentProcessResult
                {
                    IsSuccess = false,
                    TransactionReference = $"FAIL-CARD-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                    Message = "Invalid debit/credit card number. Must be 12-16 digits.",
                    MessageBn = "Invalid debit/credit card number. Must be 12-16 digits.",
                    GatewayResponse = "REJECTED_CARD_FORMAT"
                };
            }

            var refId = $"TRX-CARD-{DateTime.UtcNow:yyyyMMddHHmmss}-{new Random().Next(1000, 9999)}";
            var last4 = cardNum.Length >= 4 ? cardNum[^4..] : "XXXX";
            return new PaymentProcessResult
            {
                IsSuccess = true,
                TransactionReference = refId,
                Message = $"Card Sandbox Authorization successful for BDT {request.Amount:N2} (Card ending in {last4}).",
                MessageBn = $"Card Sandbox Authorization successful for BDT {request.Amount:N2} (Card ending in {last4}).",
                Fee = Math.Round(request.Amount * 0.02m, 2), // 2.0%
                PaidAt = DateTime.UtcNow,
                GatewayResponse = "SUCCESS_SANDBOX_SETTLED"
            };
        }
    }

    public class MockPaymentGateway : IPaymentGateway
    {
        public string GatewayName => "Mock";

        public async Task<PaymentProcessResult> ProcessPaymentAsync(PaymentProcessRequest request)
        {
            await Task.Delay(100);
            var refId = $"TRX-MOCK-{DateTime.UtcNow:yyyyMMddHHmmss}-{new Random().Next(1000, 9999)}";
            return new PaymentProcessResult
            {
                IsSuccess = true,
                TransactionReference = refId,
                Message = $"Simulated Sandbox Transaction completed for BDT {request.Amount:N2}.",
                MessageBn = $"Simulated Sandbox Transaction completed for BDT {request.Amount:N2}.",
                Fee = 0,
                PaidAt = DateTime.UtcNow,
                GatewayResponse = "SUCCESS_SANDBOX_SIMULATED"
            };
        }
    }
}
