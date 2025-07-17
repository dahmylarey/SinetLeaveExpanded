using System;
using System.ComponentModel.DataAnnotations;

namespace SinetLeaveManagement.Models
{
    public class LeaveRequest
    {
        public int Id { get; set; }

        [Required]
        public string RequestingUserId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [StringLength(500)]
        public string Reason { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";

        public DateTime RequestedAt { get; set; }

        // Navigation property
        public ApplicationUser RequestingUser { get; set; }

        //constructor to ensure RequestedAt always set
        public LeaveRequest()
        {
            RequestedAt = DateTime.UtcNow;
        }
    }
}
