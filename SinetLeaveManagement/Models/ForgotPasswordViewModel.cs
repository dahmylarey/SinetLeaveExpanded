using System.ComponentModel.DataAnnotations;

namespace SinetLeaveManagement.Models
{
    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; }
    }
}
