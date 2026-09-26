using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FixMyCity.Data;
using FixMyCity.Data.Models;
using FixMyCity.Services.Payments;
using FixMyCity.Web.Models;

namespace FixMyCity.Web.Controllers
{
    [Authorize]
    public class GatewayController : Controller
    {
        private readonly IMunicipalBillingService _billingService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly FixMyCityDbContext _context;

        public GatewayController(
            IMunicipalBillingService billingService,
            UserManager<ApplicationUser> userManager,
            FixMyCityDbContext context)
        {
            _billingService = billingService;
            _userManager = userManager;
            _context = context;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var services = await _billingService.GetActiveServicesAsync();
            var userId = _userManager.GetUserId(User);

            var model = new GatewayDashboardViewModel
            {
                Services = services
            };

            if (!string.IsNullOrEmpty(userId))
            {
                model.PendingBills = await _billingService.GetUserPendingBillsAsync(userId);
                model.RecentTransactions = await _billingService.GetUserPaymentHistoryAsync(userId, 5);
                model.TotalPaidCount = model.RecentTransactions.Count(t => t.Status == "Success");
                model.TotalPaidAmount = model.RecentTransactions.Where(t => t.Status == "Success").Sum(t => t.Amount);
            }

            return View(model);
        }

        public async Task<IActionResult> Lookup(int? serviceId = null, int? providerId = null)
        {
            var providers = await _billingService.GetAllProvidersAsync();
            if (serviceId.HasValue)
            {
                providers = providers.Where(p => p.ServiceId == serviceId.Value).ToList();
            }

            var model = new BillLookupViewModel
            {
                Providers = providers,
                ProviderId = providerId ?? providers.FirstOrDefault()?.Id ?? 0
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Lookup(BillLookupViewModel model)
        {
            model.Providers = await _billingService.GetAllProvidersAsync();
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User) ?? string.Empty;
            var bill = await _billingService.LookupBillAsync(model.ProviderId, model.ConsumerNumber, userId);

            if (bill == null)
            {
                model.ErrorMessage = "Unable to retrieve bill for the given account number. Please check the provider and number.";
                return View(model);
            }

            model.FoundBill = bill;
            return View(model);
        }

        public async Task<IActionResult> Pay(int billId)
        {
            var bill = await _billingService.GetBillByIdAsync(billId);
            if (bill == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            // Assign bill to user if anonymous or unassigned
            if (string.IsNullOrEmpty(bill.UserId) && !string.IsNullOrEmpty(currentUserId))
            {
                bill.UserId = currentUserId;
                await _context.SaveChangesAsync();
            }

            var user = await _userManager.GetUserAsync(User);

            var model = new PayBillViewModel
            {
                BillId = bill.Id,
                Bill = bill,
                CustomerPhone = user?.PhoneNumber ?? string.Empty,
                PaymentMethod = "bKash"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(PayBillViewModel model)
        {
            var bill = await _billingService.GetBillByIdAsync(model.BillId);
            if (bill == null) return NotFound();
            model.Bill = bill;

            var userId = _userManager.GetUserId(User) ?? string.Empty;

            var paymentRequest = new PaymentProcessRequest
            {
                BillId = bill.Id,
                ConsumerNumber = bill.ConsumerNumber,
                Amount = bill.Amount,
                PaymentMethod = model.PaymentMethod,
                CustomerPhone = model.CustomerPhone,
                CardNumber = model.CardNumber,
                CardExpiry = model.CardExpiry,
                CardCvv = model.CardCvv,
                PinOrOtp = model.PinOrOtp,
                Description = $"{bill.Provider?.Name} Bill Payment"
            };

            var result = await _billingService.PayBillAsync(bill.Id, userId, paymentRequest);

            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = result.Message;
                // Find transaction by ref
                var txn = _context.PaymentTransactions.FirstOrDefault(t => t.TransactionReference == result.TransactionReference);
                if (txn != null)
                {
                    return RedirectToAction(nameof(Receipt), new { transactionId = txn.Id });
                }
                return RedirectToAction(nameof(History));
            }

            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        public async Task<IActionResult> Receipt(int transactionId)
        {
            var transaction = await _billingService.GetTransactionByIdAsync(transactionId);
            if (transaction == null) return NotFound();

            var userId = _userManager.GetUserId(User);
            if (transaction.UserId != userId && !User.IsInRole("Administrator"))
            {
                return Forbid();
            }

            return View(transaction);
        }

        public async Task<IActionResult> History()
        {
            var userId = _userManager.GetUserId(User) ?? string.Empty;
            var transactions = await _billingService.GetUserPaymentHistoryAsync(userId, 50);
            return View(transactions);
        }
    }
}
