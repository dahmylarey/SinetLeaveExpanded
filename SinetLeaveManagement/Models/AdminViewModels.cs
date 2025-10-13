using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SinetLeaveManagement.Models.ViewModels
{
    public class CreateUserViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, StringLength(100)]
        public string FirstName { get; set; }

        [Required, StringLength(100)]
        public string LastName { get; set; }

        [StringLength(100)]
        public string? Department { get; set; }

        [StringLength(100)]
        public string? JobTitle { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [Required, DataType(DataType.Password), Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }

        // Selected role for the user
        public string? SelectedRole { get; set; }

        // Roles available in the system
        public List<string> Roles { get; set; } = new();
    }

    

    public class EditUserViewModel
    {
        [Required]
        public string Id { get; set; }

        [Required, StringLength(100)]
        public string FirstName { get; set; }

        [Required, StringLength(100)]
        public string LastName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [StringLength(100)]
        public string? Department { get; set; }

        [StringLength(100)]
        public string? JobTitle { get; set; }
    }
}
