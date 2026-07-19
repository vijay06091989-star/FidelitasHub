namespace FidelitasHub.Models
{
    public class AttendanceReportRow
    {
        public string EmployeeCode { get; set; } = "";
        public string EmployeeName { get; set; } = "";
        public string Department { get; set; } = "";
        public int PresentDays { get; set; }
        public int LateDays { get; set; }
        public int HalfDays { get; set; }
        public int AbsentDays { get; set; }
        public double WorkedHours { get; set; }
    }
}
