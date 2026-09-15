using FixMyCity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using FixMyCity.Data.Models;

namespace FixMyCity.Web.Controllers
{
    [Authorize]
    public class StaffController : Controller
    {
        private readonly FixMyCityDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StaffController(FixMyCityDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (!await IsStaff()) return Forbid();
            var issues = await _context.Issues
                .Include(i => i.Category)
                .Include(i => i.Reports)
                .ToListAsync();
            return View(issues);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            if (!await IsStaff()) return Forbid();
            var issue = await _context.Issues.FindAsync(id);
            if (issue == null) return NotFound();
            issue.Status = status;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Evidence(int id)
        {
            if (!await IsStaff()) return Forbid();
            ViewBag.IssueId = id;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Evidence(int id, IFormFile? photo, string? note)
        {
            if (!await IsStaff()) return Forbid();
            if (photo == null || photo.Length == 0) return RedirectToAction(nameof(Evidence), new { id });
            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "evidence");
            Directory.CreateDirectory(folder);
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(photo.FileName)}";
            await using (var stream = System.IO.File.Create(Path.Combine(folder, fileName))) await photo.CopyToAsync(stream);
            var assignment = await _context.Assignments.FirstOrDefaultAsync(a => a.IssueId == id);
            if (assignment != null) _context.EvidenceItems.Add(new FixMyCity.Data.Models.Evidence { AssignmentId = assignment.Id, PhotoPath = "/uploads/evidence/" + fileName, Note = note });
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> IsStaff() => (await _userManager.GetUserAsync(User))?.Role == "DepartmentStaff";
    }
}
