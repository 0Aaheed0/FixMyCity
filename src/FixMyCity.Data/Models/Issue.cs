namespace FixMyCity.Data.Models
{
    public class Issue
    {
        public int Id { get; set; }

        public int PriorityScore { get; set; }
        public string Status { get; set; } = "Reported"; // Reported, InProgress, Resolved, Verified

        public int CategoryId { get; set; }
        public Category? Category { get; set; }

        public List<Report> Reports { get; set; } = new();
    }
}