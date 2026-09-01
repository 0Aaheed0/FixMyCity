using FixMyCity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixMyCity.Web.Controllers
{
    public class StaffController : Controller
    {
        private readonly FixMyCityDbContext _context;

        public StaffController(FixMyCityDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var issues = await _context.Issues
                .Include(i => i.Category)
                .Include(i => i.Reports)
                .ToListAsync();
            return View(issues);
        }

        public IActionResult Evidence(int id)
        {
            ViewBag.IssueId = id;
            return View();
        }
    }
}