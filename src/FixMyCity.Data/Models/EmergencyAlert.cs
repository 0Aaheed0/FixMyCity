using System;
using System.ComponentModel.DataAnnotations;

namespace FixMyCity.Data.Models
{
    public class EmergencyAlert
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string TitleBn { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string DescriptionBn { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Severity { get; set; } = "Critical"; // Critical, High, Medium

        public DateTime StartAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpireAt { get; set; } = DateTime.UtcNow.AddDays(2);

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active, Expired, Cancelled

        public string? CreatedByUserId { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
