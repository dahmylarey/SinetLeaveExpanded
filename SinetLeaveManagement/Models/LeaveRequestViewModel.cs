using System;
using System.ComponentModel.DataAnnotations;

namespace SinetLeaveManagement.Models.ViewModels
{
    public class LeaveRequestViewModel
    {
        public int Id { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public string Reason { get; set; }

        [Required]
        [Display(Name = "Leave Type")]
        public int LeaveTypeId { get; set; }

        public IEnumerable<LeaveType> LeaveTypes { get; set; } // List of available leave types
        // etc.
    }

}
