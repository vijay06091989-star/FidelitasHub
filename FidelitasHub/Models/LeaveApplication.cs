using System;
using System.ComponentModel.DataAnnotations;

namespace FidelitasHub.Models
{
    public class LeaveApplication
    {
        [Key]
        public int LeaveApplicationId { get; set; }

        public int EmployeeId { get; set; }

        [Required]
        public string LeaveType { get; set; } = "";

        [Required]
        public DateTime FromDate { get; set; }

        [Required]
        public DateTime ToDate { get; set; }

        public bool IsMorningHalf { get; set; }

        public bool IsAfternoonHalf { get; set; }

        public decimal TotalDays { get; set; }

        public decimal CLDays { get; set; }

        public decimal LOPDays { get; set; }

        public string? Reason { get; set; }

        public string Status { get; set; } = "Pending";

        // Team Leader Approval
        public int? TeamLeaderId { get; set; }

        public string TeamLeaderStatus { get; set; } = "Pending";

        public DateTime? TeamLeaderApprovalDate { get; set; }

        public string? TeamLeaderRemarks { get; set; }


        // Manager Approval
        public int? ManagerId { get; set; }

        public string ManagerStatus { get; set; } = "Pending";

        public DateTime? ManagerApprovalDate { get; set; }

        public string? ManagerRemarks { get; set; }

        public string EmployeeRemarks { get; set; } = "";

        public DateTime AppliedOn { get; set; } = DateTime.Now;

        // Navigation Property
        public Employee? Employee { get; set; }

    }

}