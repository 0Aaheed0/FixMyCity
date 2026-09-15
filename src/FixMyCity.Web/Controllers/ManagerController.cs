using FixMyCity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using FixMyCity.Data.Models;

namespace FixMyCity.Web.Controllers
{
    [Authorize]
    public class ManagerController : Controller
    {
        private readonly FixMyCityDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ManagerController(FixMyCityDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            if (!await IsManager()) return Forbid();
            var assignments = await _context.Assignments
                .Include(a => a.Issue)
                .Include(a => a.Department)
                .Include(a => a.AssignedStaff)
                .ToListAsync();
            ViewBag.Issues = await _context.Issues.Include(i => i.Category).Where(i => i.Status != "Verified").ToListAsync();
            ViewBag.Departments = await _context.Departments.ToListAsync();
            ViewBag.Staff = await _context.Users.Where(u => u.Role == "DepartmentStaff").OrderBy(u => u.FullName).ToListAsync();
            return View(assignments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int issueId, int departmentId, string? assignedStaffUserId)
        {
            if (!await IsManager()) return Forbid();
            var issue = await _context.Issues.FindAsync(issueId);
            if (issue == null) return NotFound();
            _context.Assignments.Add(new FixMyCity.Data.Models.Assignment { IssueId = issueId, DepartmentId = departmentId, AssignedStaffUserId = assignedStaffUserId, Status = "Assigned" });
            issue.Status = "InProgress";
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAssignment(int id, string status)
        {
            if (!await IsManager()) return Forbid();
            var assignment = await _context.Assignments.Include(a => a.Issue).FirstOrDefaultAsync(a => a.Id == id);
            if (assignment == null) return NotFound();
            assignment.Status = status;
            if (assignment.Issue != null && status == "Completed") assignment.Issue.Status = "Resolved";
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> IsManager() => (await _userManager.GetUserAsync(User))?.Role == "DepartmentManager";
    }
}
