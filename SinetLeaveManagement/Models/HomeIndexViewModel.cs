using System;
using System.Collections.Generic;

namespace SinetLeaveManagement.Models
{
    public class HomeIndexViewModel
    {
        public int UnreadNotificationCount { get; set; }

        // 🟩 Add this line
        public int PendingRequestsCount { get; set; }

        // Annual Leave
        public int AnnualLeaveUsed { get; set; }
        public int AnnualLeaveTotal { get; set; }
        public int AnnualLeaveRemaining => AnnualLeaveTotal - AnnualLeaveUsed;
        public int AnnualLeavePercentage => AnnualLeaveTotal > 0
            ? (int)((double)AnnualLeaveUsed / AnnualLeaveTotal * 100)
            : 0;

        // Sick Leave
        public int SickLeaveUsed { get; set; }
        public int SickLeaveTotal { get; set; }
        public int SickLeaveRemaining => SickLeaveTotal - SickLeaveUsed;
        public int SickLeavePercentage => SickLeaveTotal > 0
            ? (int)((double)SickLeaveUsed / SickLeaveTotal * 100)
            : 0;

        // Personal Leave
        public int PersonalLeaveUsed { get; set; }
        public int PersonalLeaveTotal { get; set; }
        public int PersonalLeaveRemaining => PersonalLeaveTotal - PersonalLeaveUsed;
        public int PersonalLeavePercentage => PersonalLeaveTotal > 0
            ? (int)((double)PersonalLeaveUsed / PersonalLeaveTotal * 100)
            : 0;

        // ✅ New property for Recent Activities
        public List<RecentActivityItem> RecentActivities { get; set; } = new();

        // Recent leave requests
        public List<LeaveRequest> RecentLeaveRequests { get; set; } = new List<LeaveRequest>();
    }

    public class RecentActivityItem
    {
        public string LeaveType { get; set; }
        public DateTime DateApplied { get; set; }
        public string Duration { get; set; }
        public string Status { get; set; }
    }
}
