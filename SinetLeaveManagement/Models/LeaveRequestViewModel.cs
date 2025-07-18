using System;
using System.ComponentModel.DataAnnotations;

namespace SinetLeaveManagement.Models.ViewModels
{
    public class LeaveRequestViewModel
    {
        public int Id { get; set; }

        public string RequestingUserId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [StringLength(500)]
        public string Reason { get; set; }

        public string Status { get; set; }

        public DateTime RequestedAt { get; set; }
    }
}
