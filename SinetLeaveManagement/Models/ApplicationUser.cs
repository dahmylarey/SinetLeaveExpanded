using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SinetLeaveManagement.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required, StringLength(100)]
        public string FirstName { get; set; }

        [Required, StringLength(100)]
        public string LastName { get; set; }

        
        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(50)]
        public string? EmployeeCode { get; set; }

        [StringLength(100)]
        public string? Department { get; set; }

        [StringLength(100)]
        public string?  JobTitle { get; set; }

        [DataType(DataType.Date)]
        public DateTime? HireDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(100)]
        public string? ManagerName { get; set; }

        // Navigation property (optional)
        public EmployeeProfile? EmployeeProfile { get; set; }


        // ======================
        // ADDITIONAL PROFILE DATA
        // ======================
        public string? ProfilePicturePath { get; set; } // Stores filename or relative path


        // optional: navigation property
        public ICollection<Notification> Notifications { get; set; }

        public ICollection<LeaveRequest> LeaveRequests { get; set; }
    }
}
