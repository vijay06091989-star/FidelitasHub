using System;
using System.ComponentModel.DataAnnotations;

namespace FidelitasHub.Models
{
    public class EmployeeIdleSession
    {
        [Key]
        public int EmployeeIdleSessionId { get; set; }

        public int EmployeeId { get; set; }

        public int AttendanceId { get; set; }

        public DateTime IdleStart { get; set; }

        public DateTime? IdleEnd { get; set; }

        public int DurationSeconds { get; set; } = 0;

        [StringLength(100)]
        public string? ComputerName { get; set; }

        [StringLength(200)]
        public string? WindowsUserName { get; set; }

        public Employee? Employee { get; set; }

        public Attendance? Attendance { get; set; }
    }
}
