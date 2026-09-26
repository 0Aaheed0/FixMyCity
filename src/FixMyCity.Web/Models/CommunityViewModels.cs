using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using FixMyCity.Data.Models;

namespace FixMyCity.Web.Models
{
    public class AnnouncementsFeedViewModel
    {
        public List<Announcement> Announcements { get; set; } = new();
        public List<EmergencyAlert> ActiveEmergencyAlerts { get; set; } = new();
        public string? SelectedCategory { get; set; }
        public double? UserLatitude { get; set; }
        public double? UserLongitude { get; set; }
    }

    public class LostFoundFeedViewModel
    {
        public List<LostFoundPost> Posts { get; set; } = new();
        public string? SelectedType { get; set; } // All, Lost, Found
        public string? SelectedCategory { get; set; }
        public string? SearchQuery { get; set; }
        public int TotalCount { get; set; }
    }

    public class LostFoundCreateViewModel
    {
        [Required]
        public string PostType { get; set; } = "Lost"; // Lost, Found

        [Required(ErrorMessage = "Title is required")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? TitleBn { get; set; }

        [Required(ErrorMessage = "Please describe the item")]
        public string Description { get; set; } = string.Empty;

        public string? DescriptionBn { get; set; }

        [Required(ErrorMessage = "Please select a category")]
        public string Category { get; set; } = "Other";

        public IFormFile? Photo { get; set; }

        [Required(ErrorMessage = "Date of incident is required")]
        public DateTime EventDate { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "Location name is required (e.g. Dhanmondi 27 near bus stop)")]
        public string LocationName { get; set; } = string.Empty;

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public string? ContactInfo { get; set; }
        public string? AdditionalDetails { get; set; }
    }

    public class LostFoundDetailsViewModel
    {
        public LostFoundPost Post { get; set; } = null!;
        public bool IsOwner { get; set; }
        public bool IsAdmin { get; set; }

        [Required(ErrorMessage = "Message is required to respond")]
        public string ResponseMessage { get; set; } = string.Empty;
        public string? ResponseContactInfo { get; set; }
    }
}
