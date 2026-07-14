using System;
using System.ComponentModel.DataAnnotations;

namespace FidelitasHub.Models
{
    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }

        public int EmployeeId { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; }

        public DateTime ActionTime { get; set; }

        [StringLength(100)]
        public string Remarks { get; set; }

        [StringLength(50)]
        public string IPAddress { get; set; }

        [StringLength(100)]
        public string ComputerName { get; set; }
    }
}