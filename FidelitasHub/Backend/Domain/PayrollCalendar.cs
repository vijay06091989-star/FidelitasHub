using System;
using System.ComponentModel.DataAnnotations;

namespace FidelitasHub.Models
{
    public class PayrollCalendar
    {
        [Key]
        public int PayrollCalendarId { get; set; }

        [Required]
        public string PayrollMonth { get; set; } = "";

        [Required]
        public DateTime PeriodStart { get; set; }

        [Required]
        public DateTime PeriodEnd { get; set; }

        [Required]
        public DateTime SalaryProcessingDate { get; set; }

        public bool IsLeaveCreditProcessed { get; set; } = false;

        public string Remarks { get; set; } = "";
    }
}