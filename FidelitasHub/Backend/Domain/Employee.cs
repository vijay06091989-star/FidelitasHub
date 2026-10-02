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

        public int? ShiftId { get; set; }

        public bool IsActive { get; set; } = true;

        [Display(Name = "Enable Idle Activity Monitoring")]
        public bool EnableIdleMonitoring { get; set; } = false;

        // Employee Personal Details
        [Display(Name = "Gender")]
        [StringLength(20)]
        public string? Gender { get; set; }

        [Display(Name = "Marital Status")]
        [StringLength(30)]
        public string? MaritalStatus { get; set; }

        [Display(Name = "PAN#")]
        [StringLength(20)]
        public string? PANNumber { get; set; }

        [Display(Name = "AADHAR#")]
        [StringLength(30)]
        public string? AadharNumber { get; set; }

        [Display(Name = "DOB")]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Blood Group")]
        [StringLength(10)]
        public string? BloodGroup { get; set; }

        [Display(Name = "Father Name")]
        [StringLength(100)]
        public string? FatherName { get; set; }

        [Display(Name = "Emergency Contact Person")]
        [StringLength(100)]
        public string? EmergencyContactPerson { get; set; }

        [Display(Name = "Emergency Contact#")]
        [StringLength(20)]
        public string? EmergencyContactNumber { get; set; }

        [Display(Name = "Permanent Address")]
        [StringLength(500)]
        public string? PermanentAddress { get; set; }

        [Display(Name = "Residential Address")]
        [StringLength(500)]
        public string? ResidentialAddress { get; set; }

        [Display(Name = "Personal E-mail ID")]
        [StringLength(150)]
        public string? PersonalEmail { get; set; }
    }
}
