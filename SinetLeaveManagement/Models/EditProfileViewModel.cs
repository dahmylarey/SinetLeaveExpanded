using System.ComponentModel.DataAnnotations;

namespace SinetLeaveManagement.Models
{
    public class EditProfileViewModel
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        // ======================
        // PROFILE PICTURE SUPPORT
        // ======================
        [Display(Name = "Profile Picture")]
        public IFormFile? ProfilePicture { get; set; }

        public string? ExistingPicturePath { get; set; } // to show current picture
    }
}
