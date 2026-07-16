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

        public string Reason { get; set; } = "";

        public string Status { get; set; } = "Pending";

        public int? TeamLeaderId { get; set; }

        public DateTime? TeamLeaderApprovalDate { get; set; }

        public int? ManagerId { get; set; }

        public DateTime? ManagerApprovalDate { get; set; }

        public string Remarks { get; set; } = "";

        public DateTime AppliedOn { get; set; } = DateTime.Now;
    }
}