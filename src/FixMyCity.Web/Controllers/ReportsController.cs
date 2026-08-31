using FixMyCity.Data;
using FixMyCity.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FixMyCity.Web.Controllers
{
    public class ReportsController : Controller
    {
        private readonly FixMyCityDbContext _context;

        public ReportsController(FixMyCityDbContext context)
        {
            _context = context;
        }

        // GET: Reports
        public async Task<IActionResult> Index()
        {
            var reports = _context.Reports
                .Include(r => r.Category)
                .Include(r => r.User);
            return View(await reports.ToListAsync());
        }

        // GET: Reports/Create
        public IActionResult Create()
        {
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name");
            ViewBag.UserId = new SelectList(_context.Users, "Id", "FullName");
            return View();
        }

        // POST: Reports/Create
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

        // GET: Reports/MyReports
        public IActionResult MyReports()
        {
            return View();
        }

        // GET: Reports/Verify/5
        public IActionResult Verify(int id)
        {
            ViewBag.ReportId = id;
            return View();
        }
    }
}