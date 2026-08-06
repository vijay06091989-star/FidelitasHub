namespace FidelitasHub.Models
{
    public class TodayAttendanceViewModel
    {
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

        // KEEP THIS (existing)
        public string Status { get; set; }

        // ADD THESE (new)
        public string LeaveStatus { get; set; }

        public bool IsOnApprovedLeave { get; set; }
    }
}