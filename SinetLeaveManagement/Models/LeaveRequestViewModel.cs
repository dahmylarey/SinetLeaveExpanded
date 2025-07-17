// Models/ViewModels/LeaveRequestViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace SinetLeaveManagement.Models.ViewModels
{
    public class LeaveRequestViewModel
    {
        [Required, DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [StringLength(500)]
        public string Reason { get; set; }
    }
}
