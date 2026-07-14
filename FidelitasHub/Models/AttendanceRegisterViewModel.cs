namespace FidelitasHub.Models
{
    public class AttendanceRegisterViewModel
    {
        public string EmployeeCode { get; set; } = "";

        public string EmployeeName { get; set; } = "";

        public string Department { get; set; } = "";

        public string Shift { get; set; } = "";

        public DateTime AttendanceDate { get; set; }

        public string PunchIn { get; set; } = "";

        public string BreakStart { get; set; } = "";

        public string BreakEnd { get; set; } = "";

        public string PunchOut { get; set; } = "";

        public string WorkedTime { get; set; } = "";

        public string BreakTime { get; set; } = "";

        public string Overtime { get; set; } = "";

        public string Status { get; set; } = "";

        //==================================
        // Final Attendance Result
        //==================================

        public string AttendanceStatus { get; set; } = "";

        public string Remarks { get; set; } = "";
    }
}