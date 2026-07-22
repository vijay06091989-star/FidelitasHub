using System;
using System.ComponentModel.DataAnnotations;

namespace FidelitasHub.Models
{
    public class LeaveBalanceLedger
    {
        [Key]
        public int LedgerId { get; set; }

        public int EmployeeId { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; }

        [Required]
        public string PayrollMonth { get; set; } = "";

        [Required]
        public string TransactionType { get; set; } = "";
        // Opening
        // Monthly Credit
        // Leave Availed
        // Carry Forward
        // Manual Adjustment

        public decimal Credit { get; set; }

        public decimal Debit { get; set; }

        public decimal Balance { get; set; }

        public int? LeaveApplicationId { get; set; }

        public string Remarks { get; set; } = "";

        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }
}