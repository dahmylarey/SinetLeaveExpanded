using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SinetLeaveManagement.Models
{
    public class EmployeeProfile
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        [StringLength(20)]
        public string? EmployeeNumber { get; set; }  // Internal unique employee code

        [StringLength(100)]
        public string? Department { get; set; }

        [StringLength(100)]
        public string? JobTitle { get; set; }

        [StringLength(100)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? Country { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ContractStartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? ContractEndDate { get; set; }

        [StringLength(50)]
        public string? EmploymentStatus { get; set; } // e.g., Active, Terminated, Probation

        [StringLength(50)]
        public string? EmploymentType { get; set; } // e.g., Permanent, Contract, Intern

        public bool IsActive { get; set; } = true;
    }

}
