using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FixMyCity.Data;
using FixMyCity.Data.Models;
using FixMyCity.Services.Common;

namespace FixMyCity.Services.Notifications
{
    public interface INotificationService
    {
        Task<Notification> CreateNotificationAsync(
            string userId,
            string type,
            string title,
            string titleBn,
            string message,
            string messageBn,
            string? linkUrl = null,
            string? relatedEntityId = null,
            string? iconClass = null,
            string priority = "Normal");

        Task BroadcastEmergencyAlertAsync(EmergencyAlert alert);

        Task<int> BroadcastNearbyAnnouncementAsync(Announcement announcement);

        Task<List<Notification>> GetUserNotificationsAsync(string userId, int take = 30);

        Task<int> GetUnreadCountAsync(string userId);

        Task MarkAsReadAsync(int notificationId, string userId);

        Task MarkAllAsReadAsync(string userId);
    }

    public class NotificationService : INotificationService
    {
        private readonly FixMyCityDbContext _context;
        private readonly IGeoLocationService _geoService;

        public NotificationService(FixMyCityDbContext context, IGeoLocationService geoService)
        {
            _context = context;
            _geoService = geoService;
        }

        public async Task<Notification> CreateNotificationAsync(
            string userId,
            string type,
            string title,
            string titleBn,
            string message,
            string messageBn,
            string? linkUrl = null,
            string? relatedEntityId = null,
            string? iconClass = null,
            string priority = "Normal")
        {
            var notification = new Notification
            {
                UserId = userId,
                NotificationType = type,
                Title = title,
                TitleBn = titleBn,
                Message = message,
                MessageBn = messageBn,
                LinkUrl = linkUrl,
                RelatedEntityId = relatedEntityId,
                IconClass = iconClass ?? GetDefaultIcon(type),
                Priority = priority,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task BroadcastEmergencyAlertAsync(EmergencyAlert alert)
        {
            // Emergency alerts are broadcast to ALL citizens without geographic filtering!
            var userIds = await _context.Users.Select(u => u.Id).ToListAsync();
            var existingUserIdsNotified = await _context.Notifications
                .Where(n => n.NotificationType == "EmergencyAlert" && n.RelatedEntityId == alert.Id.ToString())
                .Select(n => n.UserId)
                .ToListAsync();

            var notifications = new List<Notification>();
            foreach (var userId in userIds)
            {
                if (existingUserIdsNotified.Contains(userId)) continue;

                notifications.Add(new Notification
                {
                    UserId = userId,
                    NotificationType = "EmergencyAlert",
                    Title = $"⚠ EMERGENCY: {alert.Title}",
                    TitleBn = $"⚠ EMERGENCY: {alert.Title}",
                    Message = alert.Description,
                    MessageBn = alert.Description,
                    LinkUrl = $"/Community/Alerts",
                    RelatedEntityId = alert.Id.ToString(),
                    IconClass = "bi-exclamation-triangle-fill",
                    Priority = "Urgent",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (notifications.Any())
            {
                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> BroadcastNearbyAnnouncementAsync(Announcement announcement)
        {
            // Geo-targeted announcement logic with Haversine distance
            // Eligible citizens are those whose LastLatitude/LastLongitude (or reported issues) are within RadiusKm
            var usersWithLocation = await _context.Users
                .Where(u => u.LastLatitude != null && u.LastLongitude != null)
                .Select(u => new { u.Id, Lat = u.LastLatitude!.Value, Lon = u.LastLongitude!.Value })
                .ToListAsync();

            // Also check users whose recent reports are in the area
            var recentReportUsers = await _context.Reports
                .Where(r => r.CreatedAt >= DateTime.UtcNow.AddMonths(-3))
                .Select(r => new { r.UserId, Lat = r.Latitude, Lon = r.Longitude })
                .ToListAsync();

            var candidates = usersWithLocation
                .Concat(recentReportUsers.Select(r => new { Id = r.UserId, r.Lat, Lon = r.Lon }))
                .ToList();

            var alreadyNotified = await _context.Notifications
                .Where(n => n.NotificationType == "Announcement" && n.RelatedEntityId == announcement.Id.ToString())
                .Select(n => n.UserId)
                .ToListAsync();

            var targetUserIds = new HashSet<string>();
            foreach (var candidate in candidates)
            {
                if (alreadyNotified.Contains(candidate.Id)) continue;

                if (_geoService.IsWithinRadius(announcement.Latitude, announcement.Longitude, candidate.Lat, candidate.Lon, announcement.RadiusKm))
                {
                    targetUserIds.Add(candidate.Id);
                }
            }

            var newNotifications = targetUserIds.Select(uid => new Notification
            {
                UserId = uid,
                NotificationType = "Announcement",
                Title = $"📢 Nearby Announcement: {announcement.Title}",
                TitleBn = $"📢 Nearby Announcement: {announcement.Title}",
                Message = announcement.Description.Length > 120 
                    ? announcement.Description[..120] + "..." 
                    : announcement.Description,
                MessageBn = string.Empty,
                LinkUrl = $"/Community/Announcements",
                RelatedEntityId = announcement.Id.ToString(),
                IconClass = "bi-megaphone-fill",
                Priority = announcement.Priority == "Urgent" ? "Urgent" : "Normal",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            if (newNotifications.Any())
            {
                _context.Notifications.AddRange(newNotifications);
                await _context.SaveChangesAsync();
            }

            return newNotifications.Count;
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(string userId, int take = 30)
        {
            return await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarkAsReadAsync(int notificationId, string userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread)
            {
                n.IsRead = true;
            }

            if (unread.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        private static string GetDefaultIcon(string type) => type switch
        {
            "IssueStatusChanged" => "bi-arrow-repeat",
            "IssueAssigned" => "bi-person-check-fill",
            "IssueResolved" => "bi-check2-circle",
            "IssueVerified" => "bi-patch-check-fill",
            "Announcement" => "bi-megaphone-fill",
            "EmergencyAlert" => "bi-exclamation-triangle-fill",
            "LostFoundResponse" => "bi-chat-dots-fill",
            "LostFoundStatus" => "bi-box-seam-fill",
            "PaymentSuccessful" => "bi-cash-coin",
            "PaymentFailed" => "bi-x-circle-fill",
            "BillDue" => "bi-receipt-cutoff",
            "MunicipalServiceUpdate" => "bi-building-fill",
            _ => "bi-bell-fill"
        };
    }
}
