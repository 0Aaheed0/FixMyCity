using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FixMyCity.Data;
using FixMyCity.Data.Models;
using FixMyCity.Services.Common;
using FixMyCity.Services.Notifications;
using FixMyCity.Web.Models;

namespace FixMyCity.Web.Controllers
{
    public class CommunityController : Controller
    {
        private readonly FixMyCityDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IGeoLocationService _geoService;
        private readonly INotificationService _notificationService;
        private readonly IWebHostEnvironment _env;

        public CommunityController(
            FixMyCityDbContext context,
            UserManager<ApplicationUser> userManager,
            IGeoLocationService geoService,
            INotificationService notificationService,
            IWebHostEnvironment env)
        {
            _context = context;
            _userManager = userManager;
            _geoService = geoService;
            _notificationService = notificationService;
            _env = env;
        }

        // ==========================================
        // 1. PUBLIC ANNOUNCEMENTS & EMERGENCY ALERTS
        // ==========================================

        public async Task<IActionResult> Announcements(string? category = null)
        {
            var now = DateTime.UtcNow;
            var query = _context.Announcements
                .Include(a => a.CreatedByUser)
                .Where(a => a.Status == "Active" && a.ExpiryDate >= now)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(category) && category != "All")
            {
                query = query.Where(a => a.Category == category);
            }

            var announcements = await query
                .OrderByDescending(a => a.Priority == "Urgent")
                .ThenByDescending(a => a.StartDate)
                .ToListAsync();

            var alerts = await _context.EmergencyAlerts
                .Where(e => e.Status == "Active" && e.ExpireAt >= now)
                .OrderByDescending(e => e.StartAt)
                .ToListAsync();

            var currentUser = await _userManager.GetUserAsync(User);

            var model = new AnnouncementsFeedViewModel
            {
                Announcements = announcements,
                ActiveEmergencyAlerts = alerts,
                SelectedCategory = category,
                UserLatitude = currentUser?.LastLatitude,
                UserLongitude = currentUser?.LastLongitude
            };

            return View(model);
        }

        public async Task<IActionResult> AnnouncementDetails(int id)
        {
            var announcement = await _context.Announcements
                .Include(a => a.CreatedByUser)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (announcement == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.LastLatitude != null && currentUser?.LastLongitude != null)
            {
                ViewBag.DistanceKm = _geoService.CalculateDistanceKm(
                    announcement.Latitude, announcement.Longitude,
                    currentUser.LastLatitude.Value, currentUser.LastLongitude.Value);
            }

            return View(announcement);
        }

        public async Task<IActionResult> Alerts()
        {
            var now = DateTime.UtcNow;
            var activeAlerts = await _context.EmergencyAlerts
                .Where(e => e.Status == "Active" && e.ExpireAt >= now)
                .OrderByDescending(e => e.Severity == "Critical")
                .ThenByDescending(e => e.StartAt)
                .ToListAsync();

            var pastAlerts = await _context.EmergencyAlerts
                .Where(e => e.Status != "Active" || e.ExpireAt < now)
                .OrderByDescending(e => e.StartAt)
                .Take(20)
                .ToListAsync();

            ViewBag.PastAlerts = pastAlerts;
            return View(activeAlerts);
        }

        // ==========================================
        // 2. LOST & FOUND COMMUNITY
        // ==========================================

        public async Task<IActionResult> LostFound(string? type = null, string? category = null, string? search = null)
        {
            var query = _context.LostFoundPosts
                .Include(p => p.User)
                .Include(p => p.Responses)
                .Where(p => !p.IsHidden)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(type) && type != "All")
            {
                query = query.Where(p => p.PostType == type);
            }

            if (!string.IsNullOrWhiteSpace(category) && category != "All")
            {
                query = query.Where(p => p.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p => p.Title.ToLower().Contains(s) || 
                                         p.Description.ToLower().Contains(s) || 
                                         p.LocationName.ToLower().Contains(s));
            }

            var posts = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            var model = new LostFoundFeedViewModel
            {
                Posts = posts,
                SelectedType = type ?? "All",
                SelectedCategory = category ?? "All",
                SearchQuery = search,
                TotalCount = posts.Count
            };

            return View(model);
        }

        [Authorize]
        public IActionResult LostFoundCreate(string type = "Lost")
        {
            var model = new LostFoundCreateViewModel
            {
                PostType = type == "Found" ? "Found" : "Lost",
                EventDate = DateTime.UtcNow,
                Latitude = 23.7806, // Default Dhaka
                Longitude = 90.4070
            };
            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LostFoundCreate(LostFoundCreateViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? photoPath = null;
            if (model.Photo != null && model.Photo.Length > 0)
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "lostfound");
                Directory.CreateDirectory(uploadsDir);

                var ext = Path.GetExtension(model.Photo.FileName).ToLowerInvariant();
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                if (allowed.Contains(ext))
                {
                    var fileName = $"{Guid.NewGuid():N}{ext}";
                    var filePath = Path.Combine(uploadsDir, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.Photo.CopyToAsync(stream);
                    }
                    photoPath = $"/uploads/lostfound/{fileName}";
                }
            }

            var post = new LostFoundPost
            {
                PostType = model.PostType,
                Title = model.Title.Trim(),
                TitleBn = model.TitleBn?.Trim(),
                Description = model.Description.Trim(),
                DescriptionBn = model.DescriptionBn?.Trim(),
                Category = model.Category,
                PhotoPath = photoPath,
                EventDate = model.EventDate,
                LocationName = model.LocationName.Trim(),
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                ContactInfo = model.ContactInfo?.Trim(),
                AdditionalDetails = model.AdditionalDetails?.Trim(),
                Status = "Active",
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                IsHidden = false
            };

            _context.LostFoundPosts.Add(post);
            await _context.SaveChangesAsync();

            // Send confirmation notification to creator
            await _notificationService.CreateNotificationAsync(
                userId: userId,
                type: "LostFoundStatus",
                title: $"{post.PostType} Item Posted: {post.Title}",
                titleBn: $"{post.PostType} Item Posted: {post.Title}",
                message: $"Your {post.PostType.ToLower()} report for '{post.Title}' is now visible in the community feed.",
                messageBn: $"Your {post.PostType.ToLower()} report for '{post.Title}' is now visible in the community feed.",
                linkUrl: $"/Community/LostFoundDetails/{post.Id}",
                relatedEntityId: post.Id.ToString(),
                iconClass: "bi-box-seam-fill"
            );

            TempData["SuccessMessage"] = "Your post has been published to the Lost & Found Community!";
            return RedirectToAction(nameof(LostFoundDetails), new { id = post.Id });
        }

        public async Task<IActionResult> LostFoundDetails(int id)
        {
            var post = await _context.LostFoundPosts
                .Include(p => p.User)
                .Include(p => p.Responses).ThenInclude(r => r.ResponderUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null || post.IsHidden) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            var isOwner = !string.IsNullOrEmpty(currentUserId) && post.UserId == currentUserId;
            var isAdmin = User.IsInRole("Administrator");

            var model = new LostFoundDetailsViewModel
            {
                Post = post,
                IsOwner = isOwner,
                IsAdmin = isAdmin
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LostFoundRespond(int postId, string responseMessage, string? responseContactInfo)
        {
            if (string.IsNullOrWhiteSpace(responseMessage))
            {
                TempData["ErrorMessage"] = "Message cannot be empty.";
                return RedirectToAction(nameof(LostFoundDetails), new { id = postId });
            }

            var post = await _context.LostFoundPosts.Include(p => p.User).FirstOrDefaultAsync(p => p.Id == postId);
            if (post == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId)) return Challenge();

            var responder = await _userManager.GetUserAsync(User);

            var response = new LostFoundResponse
            {
                PostId = postId,
                ResponderUserId = currentUserId,
                Message = responseMessage.Trim(),
                ContactInfo = responseContactInfo?.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.LostFoundResponses.Add(response);
            await _context.SaveChangesAsync();

            // Notify post creator about response
            if (post.UserId != currentUserId)
            {
                await _notificationService.CreateNotificationAsync(
                    userId: post.UserId,
                    type: "LostFoundResponse",
                    title: $"New Response on your {post.PostType} item: {post.Title}",
                    titleBn: $"New Response on your {post.PostType} item: {post.Title}",
                    message: $"{responder?.FullName ?? "A community member"} responded: \"{responseMessage.Trim()}\"",
                    messageBn: $"{responder?.FullName ?? "A community member"} responded: \"{responseMessage.Trim()}\"",
                    linkUrl: $"/Community/LostFoundDetails/{post.Id}",
                    relatedEntityId: post.Id.ToString(),
                    iconClass: "bi-chat-dots-fill",
                    priority: "High"
                );
            }

            TempData["SuccessMessage"] = "Your response was submitted to the post author.";
            return RedirectToAction(nameof(LostFoundDetails), new { id = postId });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LostFoundStatus(int id, string status)
        {
            var post = await _context.LostFoundPosts.FindAsync(id);
            if (post == null) return NotFound();

            var currentUserId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Administrator");

            if (post.UserId != currentUserId && !isAdmin)
            {
                return Forbid();
            }

            var allowed = new[] { "Active", "PotentialMatch", "Resolved", "Closed" };
            if (!allowed.Contains(status)) return BadRequest();

            post.Status = status;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Post status updated to {status}.";
            return RedirectToAction(nameof(LostFoundDetails), new { id });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LostFoundReport(int id)
        {
            var post = await _context.LostFoundPosts.FindAsync(id);
            if (post == null) return NotFound();

            post.ReportCount++;
            if (post.ReportCount >= 3)
            {
                post.IsHidden = true; // Auto-quarantine for moderation if multiple citizens flag
            }
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thank you. The post has been flagged for municipal administrator review.";
            return RedirectToAction(nameof(LostFound));
        }
    }
}
