using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FixMyCity.Data.Models;
using FixMyCity.Services.Notifications;

namespace FixMyCity.Web.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var notifications = await _notificationService.GetUserNotificationsAsync(userId, 50);
            ViewBag.UnreadCount = await _notificationService.GetUnreadCountAsync(userId);
            return View(notifications);
        }

        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Json(new { unreadCount = 0 });

            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Json(new { unreadCount = count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId))
            {
                await _notificationService.MarkAsReadAsync(id, userId);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId))
            {
                await _notificationService.MarkAllAsReadAsync(userId);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
