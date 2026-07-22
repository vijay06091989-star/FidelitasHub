using System;
using System.ComponentModel.DataAnnotations;

namespace FidelitasHub.Models
{
    public class AttendanceBreak
    {
        [Key]
        public int BreakId { get; set; }

        public int AttendanceId { get; set; }

        public DateTime BreakStart { get; set; }

        public DateTime? BreakEnd { get; set; }

        public int DurationMinutes { get; set; } = 0;
    }
}