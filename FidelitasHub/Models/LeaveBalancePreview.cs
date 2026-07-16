namespace FidelitasHub.Models
{
    public class LeaveBalancePreview
    {
        public string EmployeeCode { get; set; } = "";

        public string EmployeeName { get; set; } = "";

        public decimal LeaveBalance { get; set; }

        public string Status { get; set; } = "";
    }
}