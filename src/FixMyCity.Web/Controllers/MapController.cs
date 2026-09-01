using FixMyCity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixMyCity.Web.Controllers
{
    public class MapController : Controller
    {
        private readonly FixMyCityDbContext _context;

        public MapController(FixMyCityDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var reports = await _context.Reports
                .Include(r => r.Category)
                .ToListAsync();
            return View(reports);
        }
    }
}