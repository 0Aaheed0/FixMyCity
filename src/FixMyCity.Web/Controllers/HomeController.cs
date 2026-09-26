using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FixMyCity.Data;
using FixMyCity.Data.Models;
using FixMyCity.Web.Models;

namespace FixMyCity.Web.Controllers;

public class HomeController : Controller
{
    private readonly FixMyCityDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(FixMyCityDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        var currentUserId = _userManager.GetUserId(User);
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser?.Role == "DepartmentManager") return RedirectToAction("Index", "Manager");
        if (currentUser?.Role == "DepartmentStaff") return RedirectToAction("Index", "Staff");

        var totalReports = await _context.Reports.CountAsync();
        var totalIssues = await _context.Issues.CountAsync();
        var resolvedIssues = await _context.Issues.CountAsync(i => i.Status == "Resolved" || i.Status == "Verified");
        var pendingReports = await _context.Reports.CountAsync(r => r.IssueId == null || (r.Issue != null && r.Issue.Status != "Resolved" && r.Issue.Status != "Verified"));
        
        var myReportsCount = 0;
        var pendingBillsCount = 0;
        if (!string.IsNullOrEmpty(currentUserId))
        {
            myReportsCount = await _context.Reports.CountAsync(r => r.UserId == currentUserId);
            pendingBillsCount = await _context.CitizenBills.CountAsync(b => b.UserId == currentUserId && b.Status == "Unpaid");
        }

        var recentReports = await _context.Reports
            .Include(r => r.Category)
            .Include(r => r.User)
            .Include(r => r.Issue)
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .ToListAsync();

        var departments = await _context.Departments.Take(5).ToListAsync();
        var categories = await _context.Categories.ToListAsync();

        var now = DateTime.UtcNow;
        var emergencyAlerts = await _context.EmergencyAlerts
            .Where(e => e.Status == "Active" && e.ExpireAt >= now)
            .OrderByDescending(e => e.Severity == "Critical")
            .ThenByDescending(e => e.StartAt)
            .Take(3)
            .ToListAsync();

        var announcements = await _context.Announcements
            .Where(a => a.Status == "Active" && a.ExpiryDate >= now)
            .OrderByDescending(a => a.StartDate)
            .Take(4)
            .ToListAsync();

        var recentNotifications = !string.IsNullOrEmpty(currentUserId)
            ? await _context.Notifications
                .Where(n => n.UserId == currentUserId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync()
            : new();

        var recentLostFound = await _context.LostFoundPosts
            .Where(p => !p.IsHidden && p.Status == "Active")
            .OrderByDescending(p => p.CreatedAt)
            .Take(4)
            .ToListAsync();

        var municipalServices = await _context.MunicipalServices
            .Include(s => s.Providers.Where(p => p.IsActive))
            .Where(s => s.IsActive)
            .OrderBy(s => s.Id)
            .Take(4)
            .ToListAsync();

        var pendingBills = !string.IsNullOrEmpty(currentUserId)
            ? await _context.CitizenBills.Include(b => b.Provider).Where(b => b.UserId == currentUserId && b.Status == "Unpaid").ToListAsync()
            : new List<CitizenBill>();

        var viewModel = new DashboardViewModel
        {
            TotalReports = totalReports,
            TotalIssues = totalIssues,
            ResolvedIssues = resolvedIssues,
            PendingReports = pendingReports,
            MyReportsCount = myReportsCount,
            UserFullName = currentUser?.FullName ?? User.Identity?.Name ?? "Citizen",
            RecentReports = recentReports,
            Departments = departments,
            Categories = categories,
            ActiveEmergencyAlerts = emergencyAlerts,
            NearbyAnnouncements = announcements,
            RecentNotifications = recentNotifications,
            PendingBills = pendingBills,
            PendingBillsCount = pendingBills.Count,
            RecentLostFound = recentLostFound,
            MunicipalServices = municipalServices
        };

        return View(viewModel);
    }

    public IActionResult About()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult NotFoundPage()
    {
        return View("NotFound");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
