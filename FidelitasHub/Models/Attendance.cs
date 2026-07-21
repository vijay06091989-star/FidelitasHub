using System;
using System.ComponentModel.DataAnnotations;

namespace FidelitasHub.Models
{
    public class Attendance
    {
        [Key]
        public int AttendanceId { get; set; }

        // Employee
        public int EmployeeId { get; set; }

        // Attendance Date
        public DateTime AttendanceDate { get; set; }

        // Punch Times
        public DateTime? PunchIn { get; set; }

        public DateTime? PunchOut { get; set; }

        // Total Break Minutes
        public int TotalBreakMinutes { get; set; } = 0;

        // Total Worked Minutes (after deducting breaks)
        public int WorkedMinutes { get; set; } = 0;

        // Total Overtime Minutes
        public int OvertimeMinutes { get; set; } = 0;

        // Working / On Break / Punched Out
        [StringLength(20)]
        public string Status { get; set; } = "Working";

        // Manual / Auto
        [StringLength(20)]
        public string PunchOutMode { get; set; } = "Manual";

        // Present / Approved Leave / LOP / Absent / Holiday
        [StringLength(30)]
        public string AttendanceStatus { get; set; } = "Present";

    }
}