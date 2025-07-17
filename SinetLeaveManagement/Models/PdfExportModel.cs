using System;

namespace SinetLeaveManagement.Models
{
    public class PdfExportModel
    {
        public int Id { get; set; }
        public string RequestingUserFullName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}
