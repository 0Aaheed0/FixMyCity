using System.Security.Policy;

namespace FixMyCity.Data.Models
{
    public class Assignment
    {
        public int Id { get; set; }

        public int IssueId { get; set; }
        public Issue? Issue { get; set; }

        public int DepartmentId { get; set; }
        public Department? Department { get; set; }

        public string? AssignedStaffUserId { get; set; }
        public ApplicationUser? AssignedStaff { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Assigned"; // Assigned, InProgress, Completed

        public List<Evidence> EvidenceItems { get; set; } = new();
    }
}