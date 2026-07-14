namespace FidelitasHub.Models
{
    public class AttendanceRegisterRequest
    {
        public DateTime? FromDate { get; set; }

        public DateTime? ToDate { get; set; }

        public string Shift { get; set; } = "All";

        public string Employee { get; set; } = "";

        public string Status { get; set; } = "All";
    }
}