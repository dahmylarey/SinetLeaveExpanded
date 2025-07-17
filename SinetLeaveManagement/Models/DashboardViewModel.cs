namespace SinetLeaveManagement.Models
{
    public class DashboardViewModel
    {
        public int TotalRequests { get; set; }
        public int ApprovedCount { get; set; }
        public int PendingCount { get; set; }
        public int RejectedCount { get; set; }

        // optional: chart data
        public Dictionary<string, int> RequestsPerMonth { get; set; }
    }
}
