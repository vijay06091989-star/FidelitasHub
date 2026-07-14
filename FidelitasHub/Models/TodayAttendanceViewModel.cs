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

        public string WorkedTime { get; set; }

        public string Overtime { get; set; }

        public string Status { get; set; }
    }
}