using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FixMyCity.Data;

namespace FixMyCity.Web.Controllers
{
    public class MapController : Controller
    {
        private readonly FixMyCityDbContext _context;

        public MapController(FixMyCityDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? categoryId = null)
        {
            // CRITICAL FIX: The active problem map must ONLY display ACTIVE issues (Reported, InProgress).
            // Resolved and Verified issues must NOT display active markers on the map,
            // while preserving their full history in reports, admin, and verification.
            var query = _context.Reports
                .Include(r => r.Category)
                .Include(r => r.Issue)
                .Where(r => r.Issue == null || (r.Issue.Status != "Resolved" && r.Issue.Status != "Verified" && r.Issue.Status != "Closed"))
                .AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(r => r.CategoryId == categoryId.Value);
            }

            var activeReports = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.SelectedCategory = categoryId;
            return View(activeReports);
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveIssues(int? categoryId = null)
        {
            var query = _context.Reports
                .Include(r => r.Category)
                .Include(r => r.Issue)
                .Where(r => r.Issue == null || (r.Issue.Status != "Resolved" && r.Issue.Status != "Verified" && r.Issue.Status != "Closed"))
                .AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(r => r.CategoryId == categoryId.Value);
            }

            var items = await query.Select(r => new
            {
                id = r.Id,
                category = r.Category != null ? r.Category.Name : "General",
                description = r.Description,
                latitude = r.Latitude,
                longitude = r.Longitude,
                status = r.Issue != null ? r.Issue.Status : "Reported",
                createdAt = r.CreatedAt.ToString("MMM dd, yyyy")
            }).ToListAsync();

            return Json(items);
        }
    }
}