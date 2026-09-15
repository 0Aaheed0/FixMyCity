using FixMyCity.Data;
using FixMyCity.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace FixMyCity.Web.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly FixMyCityDbContext _context;
        private static readonly HashSet<string> AdministratorEmails = new(StringComparer.OrdinalIgnoreCase)
        {
            "yousha.cse.20230104097@aust.edu", "noman.cse.20230104088@aust.edu",
            "miraz.cse.20230104092@aust.edu", "aaheed.cse.20230104094@aust.edu"
        };

        public AdminController(FixMyCityDbContext context)
        {
            _context = context;
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
            return View();
        }

        public async Task<IActionResult> Users()
        {
            if (!IsAdministrator()) return Forbid();
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole(string id, string role)
        {
            if (!IsAdministrator()) return Forbid();
            var allowedRoles = new[] { "Citizen", "DepartmentStaff", "DepartmentManager" };
            if (!allowedRoles.Contains(role)) return BadRequest();
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.Role = role;
            await _context.SaveChangesAsync();
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
            var report = await _context.Reports.Include(r => r.Issue).FirstOrDefaultAsync(r => r.Id == reportId);
            if (report == null) return NotFound();
            if (report.Issue == null)
            {
                report.Issue = new Issue { CategoryId = report.CategoryId, Status = status, PriorityScore = 1 };
            }
            else
            {
                report.Issue.Status = status;
            }
            await _context.SaveChangesAsync();
            TempData["AdminMessage"] = $"Report #{reportId} status updated to {status}.";
            return RedirectToAction(nameof(Reports));
        }

        public async Task<IActionResult> Departments()
        {
            if (!IsAdministrator()) return Forbid();
            var departments = await _context.Departments.ToListAsync();
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
                _context.Add(category);
                await _context.SaveChangesAsync();
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

        private bool IsAdministrator()
        {
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? User.Identity?.Name ?? string.Empty;
            return AdministratorEmails.Contains(email.Trim());
        }
    }
}
