using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FixMyCity.Data;
using FixMyCity.Data.Models;
using FixMyCity.Services.Notifications;

namespace FixMyCity.Web.Controllers
{
    [Authorize]
    public class StaffController : Controller
    {
        private readonly FixMyCityDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notificationService;

        public StaffController(
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
            if (!await IsStaff()) return Forbid();
            var issues = await _context.Issues
                .Include(i => i.Category)
                .Include(i => i.Reports)
                .ToListAsync();
            ViewBag.PriorityRadar = issues.OrderByDescending(i => i.PriorityScore + i.Reports.Count).FirstOrDefault();
            return View(issues);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            if (!await IsStaff()) return Forbid();
            var issue = await _context.Issues
                .Include(i => i.Category)
                .Include(i => i.Reports)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null) return NotFound();
            var previousStatus = issue.Status;
            issue.Status = status;

            _context.StatusHistories.Add(new StatusHistory
            {
                IssueId = issue.Id,
                PreviousStatus = previousStatus,
                NewStatus = status,
                Note = "Status updated by field response crew",
                ChangedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // Notify reporting citizens
            foreach (var report in issue.Reports)
            {
                if (!string.IsNullOrEmpty(report.UserId))
                {
                    var notifType = status switch
                    {
                        "Resolved" => "IssueResolved",
                        "Verified" => "IssueVerified",
                        _ => "IssueStatusChanged"
                    };

                    await _notificationService.CreateNotificationAsync(
                        userId: report.UserId,
                        type: notifType,
                        title: $"Issue Status: {status}",
                        titleBn: $"Issue Status: {status}",
                        message: $"Field crew moved '{issue.Category?.Name}' (#{issue.Id}) to {status}.",
                        messageBn: $"Field crew moved '{issue.Category?.Name}' (#{issue.Id}) to {status}.",
                        linkUrl: $"/Reports/Verify/{report.Id}",
                        relatedEntityId: issue.Id.ToString(),
                        iconClass: status == "Resolved" ? "bi-check2-circle" : "bi-arrow-repeat"
                    );
                }
            }

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
            if (assignment != null)
            {
                _context.EvidenceItems.Add(new Evidence { AssignmentId = assignment.Id, PhotoPath = "/uploads/evidence/" + fileName, Note = note, UploadedAt = DateTime.UtcNow });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> IsStaff() => (await _userManager.GetUserAsync(User))?.Role == "DepartmentStaff";
    }
}
