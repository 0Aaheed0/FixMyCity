namespace FixMyCity.Data.Models
{
    public class Evidence
    {
        public int Id { get; set; }

        public int AssignmentId { get; set; }
        public Assignment? Assignment { get; set; }

        public string PhotoPath { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}