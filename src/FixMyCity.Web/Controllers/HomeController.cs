using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FixMyCity.Data;
using FixMyCity.Data.Models;
using FixMyCity.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

        var totalReports = await _context.Reports.CountAsync();
        var totalIssues = await _context.Issues.CountAsync();
        var resolvedIssues = await _context.Issues.CountAsync(i => i.Status == "Resolved" || i.Status == "Verified");
        var pendingReports = await _context.Reports.CountAsync(r => r.IssueId == null || (r.Issue != null && r.Issue.Status != "Resolved" && r.Issue.Status != "Verified"));
        
        var myReportsCount = 0;
        if (!string.IsNullOrEmpty(currentUserId))
        {
            myReportsCount = await _context.Reports.CountAsync(r => r.UserId == currentUserId);
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
            Categories = categories
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
