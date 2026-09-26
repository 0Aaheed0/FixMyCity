using System;
using System.ComponentModel.DataAnnotations;

namespace FixMyCity.Data.Models
{
    public class LostFoundResponse
    {
        public int Id { get; set; }

        public int PostId { get; set; }
        public LostFoundPost? Post { get; set; }

        [Required]
        public string ResponderUserId { get; set; } = string.Empty;
        public ApplicationUser? ResponderUser { get; set; }

        [Required]
        public string Message { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? ContactInfo { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
