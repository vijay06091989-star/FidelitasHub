using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FidelitasHub.Models
{
    public class EmployeeLeaveBalance
    {
        [Key]
        public int EmployeeLeaveBalanceId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee? Employee { get; set; }

        [Required]
        public decimal CurrentLeaveBalance { get; set; } = 0;

        // Payroll cycle this imported balance belongs to.
        // Existing historical rows remain null until the next proper import.
        public DateTime? BalancePeriodStart { get; set; }

        public DateTime? BalancePeriodEnd { get; set; }

        public DateTime LastUpdatedOn { get; set; } = DateTime.Now;

        public string LastUpdatedBy { get; set; } = "";
    }
}