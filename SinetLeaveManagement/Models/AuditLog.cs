using System;

namespace SinetLeaveManagement.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string Action { get; set; }

        public string PerformedBy { get; set; }   // stores user Id as string

        public int? LeaveRequestId { get; set; }

        public string Details { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
