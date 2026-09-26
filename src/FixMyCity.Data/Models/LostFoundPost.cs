using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FixMyCity.Data.Models
{
    public class LostFoundPost
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string PostType { get; set; } = "Lost"; // Lost, Found

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? TitleBn { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        public string? DescriptionBn { get; set; }

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "Other";
        // MobilePhone, Wallet, Documents, IDCard, Keys, Bag, Electronics, Pet, Bicycle, Jewelry, Other

        public string? PhotoPath { get; set; }

        public DateTime EventDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(200)]
        public string LocationName { get; set; } = string.Empty;

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        [MaxLength(200)]
        public string? ContactInfo { get; set; }

        public string? AdditionalDetails { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active, PotentialMatch, Resolved, Closed

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsHidden { get; set; } = false; // Moderation flag
        public int ReportCount { get; set; } = 0;   // User report / flag count

        public List<LostFoundResponse> Responses { get; set; } = new();
    }
}
