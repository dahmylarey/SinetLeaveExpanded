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

        // ======================
        // ADDITIONAL PROFILE DATA
        // ======================
        public string? ProfilePicturePath { get; set; } // Stores filename or relative path


        // optional: navigation property
        public ICollection<Notification> Notifications { get; set; }

        public ICollection<LeaveRequest> LeaveRequests { get; set; }
    }
}
