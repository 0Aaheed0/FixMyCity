using System.ComponentModel.DataAnnotations;

namespace FixMyCity.Data.Models
{
    public class Report
    {
        public int Id { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        public string? PhotoPath { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign keys
        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public int? IssueId { get; set; }
        public Issue? Issue { get; set; }
    }
}