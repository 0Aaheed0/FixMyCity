using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FixMyCity.Data;
using FixMyCity.Data.Models;
using FixMyCity.Services.Notifications;

namespace FixMyCity.Web.Controllers
{
    [Authorize]
    public class ManagerController : Controller
    {
        private readonly FixMyCityDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;

        public ManagerController(
            FixMyCityDbContext context, 
            UserManager<ApplicationUser> userManager,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index()
        {
            if (!await IsManager()) return Forbid();
            var assignments = await _context.Assignments
                .Include(a => a.Issue).ThenInclude(i => i!.Category)
                .Include(a => a.Department)
                .Include(a => a.AssignedStaff)
                .ToListAsync();
            ViewBag.Issues = await _context.Issues.Include(i => i.Category).Where(i => i.Status != "Verified").ToListAsync();
            ViewBag.Departments = await _context.Departments.ToListAsync();
            ViewBag.Staff = await _context.Users.Where(u => u.Role == "DepartmentStaff").OrderBy(u => u.FullName).ToListAsync();
            ViewBag.PriorityRadar = await _context.Issues.Include(i => i.Category).Include(i => i.Reports).OrderByDescending(i => i.PriorityScore + i.Reports.Count).FirstOrDefaultAsync();
            return View(assignments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int issueId, int departmentId, string? assignedStaffUserId)
        {
            if (!await IsManager()) return Forbid();
            var issue = await _context.Issues
                .Include(i => i.Category)
                .Include(i => i.Reports)
                .FirstOrDefaultAsync(i => i.Id == issueId);

            if (issue == null) return NotFound();

            var assignment = new Assignment
            {
                IssueId = issueId,
                DepartmentId = departmentId,
                AssignedStaffUserId = assignedStaffUserId,
                Status = "Assigned",
                AssignedAt = DateTime.UtcNow
            };
            _context.Assignments.Add(assignment);
            issue.Status = "InProgress";

            _context.StatusHistories.Add(new StatusHistory
            {
                IssueId = issue.Id,
                PreviousStatus = "Reported",
                NewStatus = "InProgress",
                Note = "Assigned to department by manager",
                ChangedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            var dept = await _context.Departments.FindAsync(departmentId);
            foreach (var report in issue.Reports)
            {
                if (!string.IsNullOrEmpty(report.UserId))
                {
                    await _notificationService.CreateNotificationAsync(
                        userId: report.UserId,
                        type: "IssueAssigned",
                        title: $"Issue Assigned: #{issue.Id}",
                        titleBn: $"Issue Assigned: #{issue.Id}",
                        message: $"Your reported issue '{issue.Category?.Name}' has been assigned to {dept?.Name}.",
                        messageBn: $"Your reported issue '{issue.Category?.Name}' has been assigned to {dept?.Name}.",
                        linkUrl: $"/Reports/Verify/{report.Id}",
                        relatedEntityId: issue.Id.ToString(),
                        iconClass: "bi-person-check-fill",
                        priority: "Normal"
                    );
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAssignment(int id, string status)
        {
            if (!await IsManager()) return Forbid();
            var assignment = await _context.Assignments
                .Include(a => a.Issue).ThenInclude(i => i!.Reports)
                .Include(a => a.Issue).ThenInclude(i => i!.Category)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (assignment == null) return NotFound();
            var prev = assignment.Status;
            assignment.Status = status;

            if (assignment.Issue != null && status == "Completed")
            {
                assignment.Issue.Status = "Resolved";
                _context.StatusHistories.Add(new StatusHistory
                {
                    IssueId = assignment.Issue.Id,
                    PreviousStatus = prev,
                    NewStatus = "Resolved",
                    Note = "Work order completed and verified by manager",
                    ChangedAt = DateTime.UtcNow
                });

                foreach (var report in assignment.Issue.Reports)
                {
                    if (!string.IsNullOrEmpty(report.UserId))
                    {
                        await _notificationService.CreateNotificationAsync(
                            userId: report.UserId,
                            type: "IssueResolved",
                            title: $"Issue Resolved: #{assignment.Issue.Id}",
                            titleBn: $"Issue Resolved: #{assignment.Issue.Id}",
                            message: $"Work on '{assignment.Issue.Category?.Name}' is complete. Please verify the resolution.",
                            messageBn: $"Work on '{assignment.Issue.Category?.Name}' is complete. Please verify the resolution.",
                            linkUrl: $"/Reports/Verify/{report.Id}",
                            relatedEntityId: assignment.Issue.Id.ToString(),
                            iconClass: "bi-check2-circle",
                            priority: "High"
                        );
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> IsManager() => (await _userManager.GetUserAsync(User))?.Role == "DepartmentManager";
    }
}
