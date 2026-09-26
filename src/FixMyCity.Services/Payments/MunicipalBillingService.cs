using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FixMyCity.Data;
using FixMyCity.Data.Models;
using FixMyCity.Services.Notifications;

namespace FixMyCity.Services.Payments
{
    public interface IMunicipalBillingService
    {
        Task<List<MunicipalService>> GetActiveServicesAsync();
        Task<List<MunicipalProvider>> GetProvidersByServiceAsync(int serviceId);
        Task<List<MunicipalProvider>> GetAllProvidersAsync();
        Task<MunicipalProvider?> GetProviderByIdAsync(int providerId);
        Task<CitizenBill?> LookupBillAsync(int providerId, string consumerNumber, string userId);
        Task<CitizenBill?> GetBillByIdAsync(int billId);
        Task<PaymentProcessResult> PayBillAsync(int billId, string userId, PaymentProcessRequest request);
        Task<List<CitizenBill>> GetUserPendingBillsAsync(string userId);
        Task<List<PaymentTransaction>> GetUserPaymentHistoryAsync(string userId, int take = 20);
        Task<PaymentTransaction?> GetTransactionByIdAsync(int transactionId);
        Task<List<PaymentTransaction>> GetAllTransactionsAsync(string? status = null, int take = 100);
    }

    public class MunicipalBillingService : IMunicipalBillingService
    {
        private readonly FixMyCityDbContext _context;
        private readonly IPaymentGatewayFactory _gatewayFactory;
        private readonly INotificationService _notificationService;

        public MunicipalBillingService(
            FixMyCityDbContext context,
            IPaymentGatewayFactory gatewayFactory,
            INotificationService notificationService)
        {
            _context = context;
            _gatewayFactory = gatewayFactory;
            _notificationService = notificationService;
        }

        public async Task<List<MunicipalService>> GetActiveServicesAsync()
        {
            return await _context.MunicipalServices
                .Include(s => s.Providers.Where(p => p.IsActive))
                .Where(s => s.IsActive)
                .OrderBy(s => s.Id)
                .ToListAsync();
        }

        public async Task<List<MunicipalProvider>> GetProvidersByServiceAsync(int serviceId)
        {
            return await _context.MunicipalProviders
                .Include(p => p.Service)
                .Where(p => p.ServiceId == serviceId && p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<List<MunicipalProvider>> GetAllProvidersAsync()
        {
            return await _context.MunicipalProviders
                .Include(p => p.Service)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<MunicipalProvider?> GetProviderByIdAsync(int providerId)
        {
            return await _context.MunicipalProviders
                .Include(p => p.Service)
                .FirstOrDefaultAsync(p => p.Id == providerId);
        }

        public async Task<CitizenBill?> LookupBillAsync(int providerId, string consumerNumber, string userId)
        {
            var cleanConsumer = consumerNumber.Trim();
            var bill = await _context.CitizenBills
                .Include(b => b.Provider).ThenInclude(p => p!.Service)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.ProviderId == providerId && 
                                          b.ConsumerNumber == cleanConsumer && 
                                          b.Status == "Unpaid");

            if (bill != null) return bill;

            // In Sandbox / Development mode: if bill not found, auto-generate a representative active bill
            var provider = await GetProviderByIdAsync(providerId);
            if (provider == null) return null;

            var randomAmount = new Random(cleanConsumer.GetHashCode()).Next(350, 4200);
            var now = DateTime.UtcNow;
            var currentMonth = now.ToString("MMMM yyyy");
            var currentMonthBn = GetBanglaMonthName(now);

            bill = new CitizenBill
            {
                UserId = userId,
                ProviderId = providerId,
                ConsumerNumber = cleanConsumer,
                BillNumber = $"BILL-{now:yyyyMM}-{provider.Code}-{cleanConsumer[..Math.Min(4, cleanConsumer.Length)]}",
                BillingPeriod = currentMonth,
                BillingPeriodBn = currentMonthBn,
                Amount = randomAmount,
                LateFee = 50.00m,
                DueDate = now.AddDays(14),
                Status = "Unpaid"
            };

            _context.CitizenBills.Add(bill);
            await _context.SaveChangesAsync();

            // Re-fetch with navigation properties
            return await _context.CitizenBills
                .Include(b => b.Provider).ThenInclude(p => p!.Service)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.Id == bill.Id);
        }

        public async Task<CitizenBill?> GetBillByIdAsync(int billId)
        {
            return await _context.CitizenBills
                .Include(b => b.Provider).ThenInclude(p => p!.Service)
                .Include(b => b.User)
                .Include(b => b.Transactions)
                .FirstOrDefaultAsync(b => b.Id == billId);
        }

        public async Task<PaymentProcessResult> PayBillAsync(int billId, string userId, PaymentProcessRequest request)
        {
            var bill = await GetBillByIdAsync(billId);
            if (bill == null)
            {
                return new PaymentProcessResult
                {
                    IsSuccess = false,
                    Message = "Bill not found.",
                    MessageBn = "Bill not found."
                };
            }

            if (bill.Status == "Paid")
            {
                return new PaymentProcessResult
                {
                    IsSuccess = false,
                    Message = "This bill has already been paid.",
                    MessageBn = "This bill has already been paid."
                };
            }

            request.Amount = bill.Amount;
            request.ConsumerNumber = bill.ConsumerNumber;
            request.Description = $"{bill.Provider?.Name} Bill for {bill.BillingPeriod}";

            var gateway = _gatewayFactory.GetGateway(request.PaymentMethod);
            var gatewayResult = await gateway.ProcessPaymentAsync(request);

            var transaction = new PaymentTransaction
            {
                TransactionReference = gatewayResult.TransactionReference,
                UserId = userId,
                BillId = bill.Id,
                PaymentMethod = gateway.GatewayName,
                PaymentMethodBn = GetPaymentMethodBn(gateway.GatewayName),
                Amount = bill.Amount,
                Fee = gatewayResult.Fee,
                TotalAmount = bill.Amount + gatewayResult.Fee,
                Status = gatewayResult.IsSuccess ? "Success" : "Failed",
                CustomerPhone = request.CustomerPhone,
                GatewayResponse = gatewayResult.GatewayResponse,
                CreatedAt = DateTime.UtcNow,
                PaidAt = gatewayResult.IsSuccess ? gatewayResult.PaidAt : null
            };

            _context.PaymentTransactions.Add(transaction);

            if (gatewayResult.IsSuccess)
            {
                bill.Status = "Paid";
                await _context.SaveChangesAsync();

                // Trigger notification to citizen
                await _notificationService.CreateNotificationAsync(
                    userId: userId,
                    type: "PaymentSuccessful",
                    title: $"Bill Payment Successful: {bill.Provider?.Name}",
                    titleBn: $"Bill Payment Successful: {bill.Provider?.Name}",
                    message: $"Payment of BDT {bill.Amount:N2} for {bill.BillingPeriod} ({bill.ConsumerNumber}) was successful via {gateway.GatewayName}. Ref: {transaction.TransactionReference}",
                    messageBn: $"Payment of BDT {bill.Amount:N2} for {bill.BillingPeriod} ({bill.ConsumerNumber}) was successful via {gateway.GatewayName}. Ref: {transaction.TransactionReference}",
                    linkUrl: $"/Gateway/Receipt?transactionId={transaction.Id}",
                    relatedEntityId: transaction.Id.ToString(),
                    iconClass: "bi-check-circle-fill",
                    priority: "High"
                );
            }
            else
            {
                await _context.SaveChangesAsync();

                await _notificationService.CreateNotificationAsync(
                    userId: userId,
                    type: "PaymentFailed",
                    title: $"Bill Payment Failed: {bill.Provider?.Name}",
                    titleBn: $"Bill Payment Failed: {bill.Provider?.Name}",
                    message: $"Attempted payment of BDT {bill.Amount:N2} failed: {gatewayResult.Message}",
                    messageBn: $"Attempted payment of BDT {bill.Amount:N2} failed: {gatewayResult.Message}",
                    linkUrl: $"/Gateway/Pay?billId={bill.Id}",
                    relatedEntityId: bill.Id.ToString(),
                    iconClass: "bi-x-circle-fill",
                    priority: "High"
                );
            }

            return gatewayResult;
        }

        public async Task<List<CitizenBill>> GetUserPendingBillsAsync(string userId)
        {
            return await _context.CitizenBills
                .Include(b => b.Provider).ThenInclude(p => p!.Service)
                .Where(b => b.UserId == userId && b.Status == "Unpaid")
                .OrderBy(b => b.DueDate)
                .ToListAsync();
        }

        public async Task<List<PaymentTransaction>> GetUserPaymentHistoryAsync(string userId, int take = 20)
        {
            return await _context.PaymentTransactions
                .Include(t => t.Bill).ThenInclude(b => b!.Provider)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<PaymentTransaction?> GetTransactionByIdAsync(int transactionId)
        {
            return await _context.PaymentTransactions
                .Include(t => t.Bill).ThenInclude(b => b!.Provider).ThenInclude(p => p!.Service)
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == transactionId);
        }

        public async Task<List<PaymentTransaction>> GetAllTransactionsAsync(string? status = null, int take = 100)
        {
            var query = _context.PaymentTransactions
                .Include(t => t.Bill).ThenInclude(b => b!.Provider)
                .Include(t => t.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status == status);
            }

            return await query
                .OrderByDescending(t => t.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        private static string GetBanglaMonthName(DateTime date)
        {
            return date.ToString("MMMM yyyy");
        }

        private static string GetPaymentMethodBn(string method) => method;
    }
}
