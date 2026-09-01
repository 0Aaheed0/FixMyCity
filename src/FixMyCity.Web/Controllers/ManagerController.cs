using FixMyCity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FixMyCity.Web.Controllers
{
    public class ManagerController : Controller
    {
        private readonly FixMyCityDbContext _context;

        public ManagerController(FixMyCityDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var assignments = await _context.Assignments
                .Include(a => a.Issue)
                .Include(a => a.Department)
                .Include(a => a.AssignedStaff)
                .ToListAsync();
            return View(assignments);
        }
    }
}