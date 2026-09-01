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

        public IActionResult Create()
        {
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name");
            ViewBag.UserId = new SelectList(_context.Users, "Id", "FullName");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Description,Latitude,Longitude,CategoryId,UserId")] Report report)
        {
            if (ModelState.IsValid)
            {
                report.CreatedAt = DateTime.UtcNow;
                _context.Add(report);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", report.CategoryId);
            ViewBag.UserId = new SelectList(_context.Users, "Id", "FullName", report.UserId);
            return View(report);
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