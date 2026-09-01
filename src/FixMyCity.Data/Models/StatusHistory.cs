namespace FixMyCity.Data.Models
{
    public class StatusHistory
    {
        public int Id { get; set; }

        public int IssueId { get; set; }
        public Issue? Issue { get; set; }

        public string PreviousStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public string? Note { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}