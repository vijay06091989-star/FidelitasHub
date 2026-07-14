namespace FidelitasHub.Models
{
    public class DashboardViewModel
    {
        public string EmployeeName { get; set; } = "";

        public string ShiftName { get; set; } = "";

        public string Status { get; set; } = "Not Punched In";

        public DateTime? PunchIn { get; set; }

        public DateTime? PunchOut { get; set; }

        public int TotalBreakMinutes { get; set; }

        public double WorkedMinutes { get; set; }

        // Button Permissions

        public bool CanPunchIn { get; set; } = true;

        public bool CanBreak { get; set; } = false;

        public bool CanResume { get; set; } = false;

        public bool CanPunchOut { get; set; } = false;
    }
}