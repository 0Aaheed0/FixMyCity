using System;
using System.ComponentModel.DataAnnotations;

namespace FixMyCity.Data.Models
{
    public class Announcement
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
        [MaxLength(50)]
        public string Category { get; set; } = "General"; // RoadClosure, WaterSupply, Electricity, Traffic, WasteCollection, Construction, PublicService, Weather, Event, General

        [Required]
        [MaxLength(20)]
        public string Priority { get; set; } = "Normal"; // Normal, High, Urgent

        public string? ImagePath { get; set; }

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.AddDays(7);

        // Targeted geographic center coordinates
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        // Default 2.0 km radius as required
        public double RadiusKm { get; set; } = 2.0;

        [MaxLength(200)]
        public string? TargetLocationName { get; set; }
        public string? BannerImagePath => ImagePath;

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active, Expired, Cancelled

        public string? CreatedByUserId { get; set; }
        public ApplicationUser? CreatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
