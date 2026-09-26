using System;
using System.ComponentModel.DataAnnotations;

namespace FixMyCity.Data.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        [Required]
        [MaxLength(50)]
        public string NotificationType { get; set; } = "SystemNotification";
        public string Type { get => NotificationType; set => NotificationType = value; }
        // IssueStatusChanged, IssueAssigned, IssueResolved, IssueVerified, 
        // Announcement, EmergencyAlert, LostFoundResponse, LostFoundStatus, 
        // PaymentSuccessful, PaymentFailed, BillDue, MunicipalServiceUpdate, SystemNotification

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string TitleBn { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [Required]
        public string MessageBn { get; set; } = string.Empty;

        public string? LinkUrl { get; set; }

        [MaxLength(50)]
        public string? RelatedEntityId { get; set; }

        [MaxLength(50)]
        public string? IconClass { get; set; } = "bi-bell";

        [Required]
        [MaxLength(20)]
        public string Priority { get; set; } = "Normal"; // Normal, High, Urgent

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
