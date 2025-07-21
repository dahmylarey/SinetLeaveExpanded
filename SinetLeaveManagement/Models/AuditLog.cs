using System;

namespace SinetLeaveManagement.Models
{
    public class AuditLog
    {
        public int Id { get; set; }


        public string Action { get; set; }

        public string PerformedByUserId { get; set; }  // FK to ApplicationUser
        public ApplicationUser PerformedByUser { get; set; }  // nav property

        public int? LeaveRequestId { get; set; }
        public LeaveRequest LeaveRequest { get; set; }

        public string Details { get; set; }

        public DateTime Timestamp { get; set; }
    }
}
