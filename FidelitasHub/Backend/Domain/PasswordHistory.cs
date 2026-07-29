using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FidelitasHub.Models
{
    public class PasswordHistory
    {
        [Key]
        public int PasswordHistoryId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; }

        [Required]
        [StringLength(100)]
        public string Password { get; set; }

        public DateTime ChangedOn { get; set; } = DateTime.Now;
    }
}