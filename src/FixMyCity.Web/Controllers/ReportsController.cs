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

        public async Task<IActionResult> Index()
        {
            var reports = _context.Reports
                .Include(r => r.Category)
                .Include(r => r.User);
            return View(await reports.ToListAsync());
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
                _context.Add(report);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Issue report submitted successfully! Thank you for helping fix your city.";
                return RedirectToAction(nameof(Index));
            }
            return await CreateFormViewAsync(report);
        }

        [Authorize]
        public async Task<IActionResult> MyReports()
        {
            var userId = _userManager.GetUserId(User);
            var myReports = await _context.Reports
                .Include(r => r.Category)
                .Include(r => r.Issue)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return View(myReports);
        }

        public async Task<IActionResult> Verify(int id)
        {
            var report = await _context.Reports
                .Include(r => r.Category)
                .Include(r => r.Issue)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (report == null) return NotFound();
            return View(report);
        }
    }
}
