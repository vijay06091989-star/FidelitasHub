using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FidelitasHub.Models
{
    public class LeaveApplicationViewModel
    {
        public int LeaveApplicationId { get; set; }

        public int EmployeeId { get; set; }

        public List<SelectListItem> Employees { get; set; } = new();

        public string LeaveType { get; set; } = "";

        public List<SelectListItem> LeaveTypes { get; set; } = new();

        [Required]
        public DateTime FromDate { get; set; } = DateTime.Today;

        [Required]
        public DateTime ToDate { get; set; } = DateTime.Today;

        public string LeaveSession { get; set; } = "Full";

        public string? Reason { get; set; }

        // Calculated
        public decimal TotalDays { get; set; }

        public decimal CLDays { get; set; }

        public decimal LOPDays { get; set; }

        public decimal LeaveBalance { get; set; }

        public string Status { get; set; } = "";
    }
}