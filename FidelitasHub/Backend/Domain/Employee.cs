using System;
using System.ComponentModel.DataAnnotations;

namespace FidelitasHub.Models
{
    public class Employee
    {
        [Key]
        public int EmployeeId { get; set; }

        [Required]
        [Display(Name = "Employee Code")]
        [StringLength(10)]
        public string EmployeeCode { get; set; }

        [Required]
        [Display(Name = "Employee Name")]
        [StringLength(100)]
        public string EmployeeName { get; set; }

        [Required]
        [StringLength(100)]
        public string Password { get; set; }

        // NEW: Force user to change password on first login
        public bool MustChangePassword { get; set; } = true;

        [Display(Name = "Password Last Changed")]
        public DateTime PasswordLastChanged { get; set; } = DateTime.Today;

        [Display(Name = "Department")]
        [StringLength(100)]
        public string Department { get; set; }

        [Display(Name = "Designation")]
        [StringLength(100)]
        public string Designation { get; set; }

        [Display(Name = "Email Address")]
        [StringLength(100)]
        public string Email { get; set; }

        [Display(Name = "Mobile Number")]
        [StringLength(20)]
        public string Mobile { get; set; }

        [Display(Name = "Date Joined")]
        public DateTime DateJoined { get; set; } = DateTime.Today;

        [Display(Name = "Role")]
        [StringLength(50)]
        public string Role { get; set; }

        [Display(Name = "Reporting Manager")]
        public int? ReportingManagerId { get; set; }

        [Display(Name = "Reporting Team Leader")]
        public int? ReportingTeamLeaderId { get; set; }

        public Employee? ReportingManager { get; set; }
        public Employee? ReportingTeamLeader { get; set; }

        public int ShiftId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}