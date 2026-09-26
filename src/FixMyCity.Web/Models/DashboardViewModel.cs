using System.Collections.Generic;
using FixMyCity.Data.Models;

namespace FixMyCity.Web.Models
{
    public class DashboardViewModel
    {
        public int TotalReports { get; set; }
        public int TotalIssues { get; set; }
        public int ResolvedIssues { get; set; }
        public int PendingReports { get; set; }
        public int MyReportsCount { get; set; }
        public string UserFullName { get; set; } = string.Empty;
        public List<Report> RecentReports { get; set; } = new();
        public List<Department> Departments { get; set; } = new();
        public List<Category> Categories { get; set; } = new();

        // Smart Municipal Portal Elements
        public List<EmergencyAlert> ActiveEmergencyAlerts { get; set; } = new();
        public List<EmergencyAlert> ActiveAlerts => ActiveEmergencyAlerts;
        public List<Announcement> NearbyAnnouncements { get; set; } = new();
        public List<Notification> RecentNotifications { get; set; } = new();
        public List<Notification> UnreadNotifications => RecentNotifications;
        public List<CitizenBill> PendingBills { get; set; } = new();
        public int PendingBillsCount { get; set; }
        public List<LostFoundPost> RecentLostFound { get; set; } = new();
        public List<MunicipalService> MunicipalServices { get; set; } = new();
    }
}
