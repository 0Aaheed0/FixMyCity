using FixMyCity.Data;
using FixMyCity.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FixMyCity.Web.Controllers
{
    public class ReportsController : Controller
    {
        private readonly FixMyCityDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(FixMyCityDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string? status = null, int? categoryId = null)
        {
            var query = _context.Reports
                .Include(r => r.Category)
                .Include(r => r.User)
                .Include(r => r.Issue)
                .AsQueryable();
            if (categoryId.HasValue) query = query.Where(r => r.CategoryId == categoryId.Value);
            if (!string.IsNullOrWhiteSpace(status)) query = query.Where(r => r.Issue == null ? status == "Reported" : r.Issue.Status == status);
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedCategory = categoryId;
            return View(await query.OrderByDescending(r => r.CreatedAt).ToListAsync());
        }

        public async Task<IActionResult> Create()
        {
            await EnsureDefaultCategoriesAsync();
            return await CreateFormViewAsync();
        }

        private async Task EnsureDefaultCategoriesAsync()
        {
            var defaults = new[]
            {
                ("Pothole / Road Damage", "Roads & Transport"),
                ("Broken Traffic Signal", "Roads & Transport"),
                ("Garbage / Waste", "Waste Management"),
                ("Water Leak / Drainage", "Water & Utilities"),
                ("Broken Streetlight", "Public Lighting"),
                ("Damaged Public Property", "Roads & Transport")
            };
            var existing = await _context.Categories.Select(c => c.Name).ToListAsync();
            var missing = defaults
                .Where(item => !existing.Contains(item.Item1, StringComparer.OrdinalIgnoreCase))
                .Select(item => new Category { Name = item.Item1, ResponsibleDepartment = item.Item2 });
            if (missing.Any())
            {
                _context.Categories.AddRange(missing);
                await _context.SaveChangesAsync();
            }
        }

        private async Task<IActionResult> CreateFormViewAsync(Report? report = null)
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();
            var users = await _context.Users
                .AsNoTracking()
                .OrderBy(u => u.FullName)
                .ToListAsync();
            ViewBag.CategoryId = new SelectList(categories, "Id", "Name", report?.CategoryId);
            ViewBag.UserId = new SelectList(users, "Id", "FullName", report?.UserId);
            return View(report);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Description,Latitude,Longitude,CategoryId,UserId")] Report report, IFormFile? photo)
        {
            await EnsureDefaultCategoriesAsync();
            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUserId = _userManager.GetUserId(User);
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    report.UserId = currentUserId;
                    ModelState.Remove(nameof(report.UserId));
                }
            }
            else if (string.IsNullOrEmpty(report.UserId))
            {
                var firstUser = await _context.Users.FirstOrDefaultAsync();
                if (firstUser != null)
                {
                    report.UserId = firstUser.Id;
                    ModelState.Remove(nameof(report.UserId));
                }
            }

            if (photo != null && photo.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "reports");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(photo.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(fileStream);
                }
                report.PhotoPath = "/uploads/reports/" + uniqueFileName;
            }

            if (ModelState.IsValid)
            {
                report.CreatedAt = DateTime.UtcNow;
                var nearby = await _context.Reports.Include(r => r.Issue)
                    .Where(r => r.CategoryId == report.CategoryId && Math.Abs(r.Latitude - report.Latitude) < 0.00045 && Math.Abs(r.Longitude - report.Longitude) < 0.00045)
                    .FirstOrDefaultAsync();
                if (nearby?.Issue != null)
                {
                    nearby.Issue.PriorityScore += 2;
                    report.IssueId = nearby.IssueId;
                }
                _context.Add(report);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Issue report submitted successfully! Thank you for helping fix your city.";
                return RedirectToAction(nameof(Index));
            }
            return await CreateFormViewAsync(report);
        }

        [Authorize]
        public async Task<IActionResult> MyReports(string? status = null)
        {
            var userId = _userManager.GetUserId(User);
            var myReports = await _context.Reports
                .Include(r => r.Category)
                .Include(r => r.Issue).ThenInclude(i => i!.Reports)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            if (!string.IsNullOrWhiteSpace(status)) myReports = myReports.Where(r => (r.Issue?.Status ?? "Reported") == status).ToList();
            ViewBag.SelectedStatus = status;
            return View(myReports);
        }

        public async Task<IActionResult> Verify(int id)
        {
            var report = await _context.Reports
                .Include(r => r.Category)
                .Include(r => r.Issue)
                .ThenInclude(i => i!.Reports)
                .Include(r => r.Issue)
                .ThenInclude(i => i!.Reports)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (report == null) return NotFound();
            ViewBag.StatusHistory = await _context.StatusHistories.Where(h => h.IssueId == report.IssueId).OrderBy(h => h.ChangedAt).ToListAsync();
            ViewBag.Evidence = await _context.EvidenceItems.Include(e => e.Assignment).Where(e => report.IssueId != null && e.Assignment!.IssueId == report.IssueId).OrderByDescending(e => e.UploadedAt).ToListAsync();
            return View(report);
        }

        [Authorize]
        public async Task<IActionResult> Notifications()
        {
            var userId = _userManager.GetUserId(User);
            var issueIds = await _context.Reports.Where(r => r.UserId == userId && r.IssueId != null).Select(r => r.IssueId!.Value).Distinct().ToListAsync();
            return View(await _context.StatusHistories.Where(h => issueIds.Contains(h.IssueId)).OrderByDescending(h => h.ChangedAt).Take(30).ToListAsync());
        }

        [Authorize]
        public async Task<IActionResult> Impact()
        {
            var userId = _userManager.GetUserId(User);
            var reports = await _context.Reports.Include(r => r.Issue).Where(r => r.UserId == userId).ToListAsync();
            ViewBag.Total = reports.Count;
            ViewBag.Resolved = reports.Count(r => r.Issue?.Status is "Resolved" or "Verified");
            ViewBag.Active = reports.Count(r => r.Issue?.Status is not "Resolved" and not "Verified");
            ViewBag.Score = reports.Count * 10 + reports.Count(r => r.Issue?.Status is "Resolved" or "Verified") * 20;
            ViewBag.CommunityReach = reports.Sum(r => r.Issue?.Reports.Count ?? 1);
            return View();
        }

        [Authorize]
        public async Task<IActionResult> ExportMyReports()
        {
            var userId = _userManager.GetUserId(User);
            var reports = await _context.Reports.Include(r => r.Category).Include(r => r.Issue).Where(r => r.UserId == userId).OrderByDescending(r => r.CreatedAt).ToListAsync();
            var csv = "Report Id,Category,Description,Status,Latitude,Longitude,Reported At\r\n" + string.Join("\r\n", reports.Select(r => $"{r.Id},\"{r.Category?.Name?.Replace("\"", "\"\"")}\",\"{r.Description.Replace("\"", "\"\"")}\",{r.Issue?.Status ?? "Reported"},{r.Latitude},{r.Longitude},{r.CreatedAt:yyyy-MM-dd HH:mm}"));
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "fixmycity-my-reports.csv");
        }
    }
}
