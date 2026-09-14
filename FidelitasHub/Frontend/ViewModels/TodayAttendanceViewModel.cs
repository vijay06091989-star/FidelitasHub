namespace FidelitasHub.Models
{
    public class TodayAttendanceViewModel
    {
        public int? AttendanceId { get; set; }

        public string EmployeeCode { get; set; }

        public string EmployeeName { get; set; }

        public string Department { get; set; }

        public string Shift { get; set; }

        public string PunchIn { get; set; }

        public string PunchOut { get; set; }

        public string BreakStart { get; set; }

        public string BreakEnd { get; set; }

        public string TotalBreak { get; set; }

        public string WorkedTime { get; set; }

        public bool IsIdleMonitoringEnabled { get; set; }

        public bool IsCurrentlyIdle { get; set; }

        public string CurrentActivity { get; set; } = "--";

        public string TotalIdle { get; set; } = "--";

        // KEEP THIS (existing)
        public string Status { get; set; }

        // ADD THESE (new)
        public string LeaveStatus { get; set; }

        public bool IsOnApprovedLeave { get; set; }

        public LeaveApplication? ApprovedLeave { get; set; }

        public Shift? ShiftModel { get; set; }
    }
}