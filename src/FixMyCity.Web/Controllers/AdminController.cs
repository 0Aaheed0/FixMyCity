using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FixMyCity.Data;
using FixMyCity.Data.Models;
using FixMyCity.Services.Notifications;

namespace FixMyCity.Web.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly FixMyCityDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        private static readonly HashSet<string> AdministratorEmails = new(StringComparer.OrdinalIgnoreCase)
        {
            "yousha.cse.20230104097@aust.edu", "noman.cse.20230104088@aust.edu",
            "miraz.cse.20230104092@aust.edu", "aaheed.cse.20230104094@aust.edu",
            "nabdullahal83@gmail.com", "admin@fixmycity.com"
        };

        public AdminController(
            FixMyCityDbContext context,
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _notificationService = notificationService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (!IsAdministrator()) return Forbid();

            ViewBag.TotalReports = await _context.Reports.CountAsync();
            ViewBag.TotalIssues = await _context.Issues.CountAsync();
            ViewBag.ReportedIssues = await _context.Issues.CountAsync(i => i.Status == "Reported");
            ViewBag.InProgressIssues = await _context.Issues.CountAsync(i => i.Status == "InProgress");
            ViewBag.ResolvedIssues = await _context.Issues.CountAsync(i => i.Status == "Resolved");
            ViewBag.VerifiedIssues = await _context.Issues.CountAsync(i => i.Status == "Verified");
            ViewBag.TotalUsers = await _context.Users.CountAsync();

            // Smart Portal Additions
            ViewBag.ActiveAnnouncements = await _context.Announcements.CountAsync(a => a.Status == "Active");
            ViewBag.ActiveEmergencyAlerts = await _context.EmergencyAlerts.CountAsync(e => e.Status == "Active");
            ViewBag.LostFoundPosts = await _context.LostFoundPosts.CountAsync(p => p.Status == "Active");
            ViewBag.TotalTransactions = await _context.PaymentTransactions.CountAsync();
            ViewBag.TotalPaymentsVolume = await _context.PaymentTransactions.Where(t => t.Status == "Success").SumAsync(t => (decimal?)t.Amount) ?? 0;

            return View();
        }

        public async Task<IActionResult> Users()
        {
            if (!IsAdministrator()) return Forbid();
            var users = await _context.Users.OrderBy(u => u.FullName).ToListAsync();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole(string id, string role)
        {
            if (!IsAdministrator()) return Forbid();
            var allowedRoles = new[] { "Citizen", "DepartmentStaff", "DepartmentManager", "Administrator" };
            if (!allowedRoles.Contains(role)) return BadRequest();
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.Role = role;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Role for {user.FullName} updated to {role}.";
            return RedirectToAction(nameof(Users));
        }

        public async Task<IActionResult> Reports()
        {
            if (!IsAdministrator()) return Forbid();
            var reports = await _context.Reports
                .Include(r => r.Category)
                .Include(r => r.User)
                .Include(r => r.Issue)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return View(reports);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReportStatus(int reportId, string status)
        {
            if (!IsAdministrator()) return Forbid();
            var allowed = new[] { "Reported", "InProgress", "Resolved", "Verified" };
            if (!allowed.Contains(status)) return BadRequest();
            var report = await _context.Reports.Include(r => r.Issue).Include(r => r.Category).FirstOrDefaultAsync(r => r.Id == reportId);
            if (report == null) return NotFound();
            var previousStatus = report.Issue?.Status ?? "Reported";
            if (report.Issue == null)
            {
                report.Issue = new Issue { CategoryId = report.CategoryId, Status = status, PriorityScore = 1 };
            }
            else
            {
                report.Issue.Status = status;
            }
            _context.StatusHistories.Add(new StatusHistory { Issue = report.Issue, PreviousStatus = previousStatus, NewStatus = status, Note = "Status updated by municipal administrator" });
            await _context.SaveChangesAsync();

            // Notify user
            if (!string.IsNullOrEmpty(report.UserId))
            {
                await _notificationService.CreateNotificationAsync(
                    userId: report.UserId,
                    type: "IssueStatusChanged",
                    title: $"Issue Status Updated: #{report.Id}",
                    titleBn: $"Issue Status Updated: #{report.Id}",
                    message: $"Administrator updated the status of '{report.Category?.Name}' to {status}.",
                    messageBn: $"Administrator updated the status of '{report.Category?.Name}' to {status}.",
                    linkUrl: $"/Reports/Verify/{report.Id}",
                    relatedEntityId: report.Id.ToString(),
                    iconClass: "bi-shield-check"
                );
            }

            TempData["AdminMessage"] = $"Report #{reportId} status updated to {status}.";
            return RedirectToAction(nameof(Reports));
        }

        public async Task<IActionResult> Departments()
        {
            if (!IsAdministrator()) return Forbid();
            var departments = await _context.Departments.Include(d => d.Categories).ToListAsync();
            return View(departments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Departments(Department department)
        {
            if (!IsAdministrator()) return Forbid();
            if (ModelState.IsValid)
            {
                _context.Add(department);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Department '{department.Name}' created successfully.";
            }
            return RedirectToAction(nameof(Departments));
        }

        public async Task<IActionResult> Categories()
        {
            if (!IsAdministrator()) return Forbid();
            var categories = await _context.Categories.Include(c => c.Department).ToListAsync();
            ViewBag.Departments = await _context.Departments.ToListAsync();
            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Categories(Category category)
        {
            if (!IsAdministrator()) return Forbid();
            if (ModelState.IsValid)
            {
                if (category.DepartmentId.HasValue)
                {
                    var dep = await _context.Departments.FindAsync(category.DepartmentId.Value);
                    category.ResponsibleDepartment = dep?.Name;
                }
                _context.Add(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Category '{category.Name}' added successfully.";
            }
            return RedirectToAction(nameof(Categories));
        }

        public async Task<IActionResult> Analytics()
        {
            if (!IsAdministrator()) return Forbid();
            var byCategory = await _context.Reports
                .Include(r => r.Category)
                .GroupBy(r => r.Category!.Name)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync();
            ViewBag.ByCategory = byCategory;
            return View();
        }

        // ==========================================
        // SMART MUNICIPAL PORTAL: ANNOUNCEMENTS
        // ==========================================

        public async Task<IActionResult> Announcements()
        {
            if (!IsAdministrator()) return Forbid();
            var announcements = await _context.Announcements
                .Include(a => a.CreatedByUser)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            return View(announcements);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAnnouncement(Announcement announcement)
        {
            if (!IsAdministrator()) return Forbid();

            announcement.CreatedByUserId = _userManager.GetUserId(User);
            announcement.CreatedAt = DateTime.UtcNow;
            if (string.IsNullOrEmpty(announcement.TitleBn)) announcement.TitleBn = announcement.Title;
            if (string.IsNullOrEmpty(announcement.DescriptionBn)) announcement.DescriptionBn = announcement.Description;
            if (announcement.RadiusKm <= 0) announcement.RadiusKm = 2.0;

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            // Broadcast to citizens within 2KM radius
            var notifiedCount = await _notificationService.BroadcastNearbyAnnouncementAsync(announcement);

            TempData["SuccessMessage"] = $"Announcement published! {notifiedCount} citizens within {announcement.RadiusKm} km radius were notified.";
            return RedirectToAction(nameof(Announcements));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExpireAnnouncement(int id)
        {
            if (!IsAdministrator()) return Forbid();
            var a = await _context.Announcements.FindAsync(id);
            if (a != null)
            {
                a.Status = "Expired";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Announcement marked as expired.";
            }
            return RedirectToAction(nameof(Announcements));
        }

        // ==========================================
        // SMART MUNICIPAL PORTAL: EMERGENCY ALERTS
        // ==========================================

        public async Task<IActionResult> EmergencyAlerts()
        {
            if (!IsAdministrator()) return Forbid();
            var alerts = await _context.EmergencyAlerts
                .Include(a => a.CreatedByUser)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            return View(alerts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmergencyAlert(EmergencyAlert alert)
        {
            if (!IsAdministrator()) return Forbid();

            alert.CreatedByUserId = _userManager.GetUserId(User);
            alert.CreatedAt = DateTime.UtcNow;
            alert.Status = "Active";
            if (string.IsNullOrEmpty(alert.TitleBn)) alert.TitleBn = alert.Title;
            if (string.IsNullOrEmpty(alert.DescriptionBn)) alert.DescriptionBn = alert.Description;

            _context.EmergencyAlerts.Add(alert);
            await _context.SaveChangesAsync();

            // Emergency alerts have NO geographic boundary - broadcast to EVERY citizen
            await _notificationService.BroadcastEmergencyAlertAsync(alert);

            TempData["SuccessMessage"] = "Emergency Alert published city-wide! All citizens notified immediately.";
            return RedirectToAction(nameof(EmergencyAlerts));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelEmergencyAlert(int id)
        {
            if (!IsAdministrator()) return Forbid();
            var alert = await _context.EmergencyAlerts.FindAsync(id);
            if (alert != null)
            {
                alert.Status = "Cancelled";
                alert.ExpireAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Emergency Alert has been cancelled.";
            }
            return RedirectToAction(nameof(EmergencyAlerts));
        }

        // ==========================================
        // SMART MUNICIPAL PORTAL: LOST & FOUND MODERATION
        // ==========================================

        public async Task<IActionResult> LostFound()
        {
            if (!IsAdministrator()) return Forbid();
            var posts = await _context.LostFoundPosts
                .Include(p => p.User)
                .Include(p => p.Responses)
                .OrderByDescending(p => p.ReportCount)
                .ThenByDescending(p => p.CreatedAt)
                .ToListAsync();
            return View(posts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLostFoundHide(int id)
        {
            if (!IsAdministrator()) return Forbid();
            var post = await _context.LostFoundPosts.FindAsync(id);
            if (post != null)
            {
                post.IsHidden = !post.IsHidden;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = post.IsHidden ? "Post has been hidden from public feed." : "Post has been restored to public feed.";
            }
            return RedirectToAction(nameof(LostFound));
        }

        // ==========================================
        // SMART MUNICIPAL PORTAL: SERVICES & PROVIDERS
        // ==========================================

        public async Task<IActionResult> MunicipalServices()
        {
            if (!IsAdministrator()) return Forbid();
            var services = await _context.MunicipalServices
                .Include(s => s.Providers)
                .OrderBy(s => s.Name)
                .ToListAsync();
            return View(services);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateService(MunicipalService service)
        {
            if (!IsAdministrator()) return Forbid();
            if (string.IsNullOrEmpty(service.NameBn)) service.NameBn = service.Name;
            if (string.IsNullOrEmpty(service.DescriptionBn)) service.DescriptionBn = service.Description ?? string.Empty;
            ModelState.Clear();
            TryValidateModel(service);
            if (ModelState.IsValid)
            {
                _context.MunicipalServices.Add(service);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Municipal service '{service.Name}' created.";
            }
            return RedirectToAction(nameof(MunicipalServices));
        }

        public async Task<IActionResult> MunicipalProviders()
        {
            if (!IsAdministrator()) return Forbid();
            var providers = await _context.MunicipalProviders
                .Include(p => p.Service)
                .OrderBy(p => p.Name)
                .ToListAsync();
            ViewBag.Services = await _context.MunicipalServices.Where(s => s.IsActive).ToListAsync();
            return View(providers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProvider(MunicipalProvider provider)
        {
            if (!IsAdministrator()) return Forbid();
            if (string.IsNullOrEmpty(provider.NameBn)) provider.NameBn = provider.Name;
            if (string.IsNullOrEmpty(provider.AccountNumberLabelBn)) provider.AccountNumberLabelBn = provider.AccountNumberLabel;
            ModelState.Clear();
            TryValidateModel(provider);
            if (ModelState.IsValid)
            {
                _context.MunicipalProviders.Add(provider);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Provider '{provider.Name}' created successfully.";
            }
            return RedirectToAction(nameof(MunicipalProviders));
        }

        // ==========================================
        // SMART MUNICIPAL PORTAL: TRANSACTIONS
        // ==========================================

        public async Task<IActionResult> Transactions(string? status = null)
        {
            if (!IsAdministrator()) return Forbid();
            var query = _context.PaymentTransactions
                .Include(t => t.Bill).ThenInclude(b => b!.Provider)
                .Include(t => t.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(t => t.Status == status);
            }

            var transactions = await query.OrderByDescending(t => t.CreatedAt).Take(100).ToListAsync();
            ViewBag.SelectedStatus = status;
            ViewBag.TotalSuccessful = await _context.PaymentTransactions.CountAsync(t => t.Status == "Success");
            ViewBag.TotalFailed = await _context.PaymentTransactions.CountAsync(t => t.Status == "Failed");
            return View(transactions);
        }

        // ==========================================
        // LANGUAGE SWITCHER
        // ==========================================

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true }
            );

            if (Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        private bool IsAdministrator()
        {
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? User.Identity?.Name ?? string.Empty;
            var user = _context.Users.FirstOrDefault(u => u.Email == email || u.UserName == email);
            return (user != null && user.Role == "Administrator") || AdministratorEmails.Contains(email.Trim());
        }
    }
}
