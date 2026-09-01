using FixMyCity.Data;
using FixMyCity.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixMyCity.Web.Controllers
{
    public class AdminController : Controller
    {
        private readonly FixMyCityDbContext _context;

        public AdminController(FixMyCityDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalReports = await _context.Reports.CountAsync();
            ViewBag.TotalIssues = await _context.Issues.CountAsync();
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            return View();
        }

        public async Task<IActionResult> Users()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        public async Task<IActionResult> Departments()
        {
            var departments = await _context.Departments.ToListAsync();
            return View(departments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Departments(Department department)
        {
            if (ModelState.IsValid)
            {
                _context.Add(department);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Departments));
        }

        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories.Include(c => c.Department).ToListAsync();
            ViewBag.Departments = await _context.Departments.ToListAsync();
            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Categories(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Categories));
        }

        public async Task<IActionResult> Analytics()
        {
            var byCategory = await _context.Reports
                .Include(r => r.Category)
                .GroupBy(r => r.Category!.Name)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync();
            ViewBag.ByCategory = byCategory;
            return View();
        }
    }
}