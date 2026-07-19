using Microsoft.AspNetCore.Mvc.Rendering;

namespace FidelitasHub.Models
{
    public class AttendanceCorrectionViewModel
    {
        public int EmployeeId { get; set; }

        public DateTime AttendanceDate { get; set; }
            = DateTime.Today;

        public List<SelectListItem> Employees
            = new();

        public string? EmployeeCode { get; set; }

        public string? EmployeeName { get; set; }

        public string? Department { get; set; }

        public string? Shift { get; set; }

        public DateTime? PunchIn { get; set; }

        public DateTime? PunchOut { get; set; }

        public int BreakMinutes { get; set; }

        public string? Status { get; set; }

        public string? Reason { get; set; }

        public TimeSpan? NewPunchInTime { get; set; }

        public TimeSpan? NewPunchOutTime { get; set; }

        public int NewBreakMinutes { get; set; }

        public string? CorrectionType { get; set; }

        public bool ShowPunchIn { get; set; }

        public bool ShowPunchOut { get; set; }

        public bool ShowBreakMinutes { get; set; }

    }
}