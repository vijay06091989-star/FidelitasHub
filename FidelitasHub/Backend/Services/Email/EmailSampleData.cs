namespace FidelitasHub.Services.Email
{
    public static class EmailSampleData
    {
        public static Dictionary<string, string> Get()
        {
            return new Dictionary<string, string>
            {
                { "EmployeeName", "John Smith" },
                { "EmployeeCode", "EMP001" },
                { "Department", "Information Technology" },
                { "Designation", "Software Engineer" },
                { "CompanyName", "Fidelitas Hub" },
                { "AttendanceDate", DateTime.Today.ToString("dd-MMM-yyyy") },
                { "PunchIn", "09:00 AM" },
                { "PunchOut", "06:00 PM" },
                { "LeaveType", "Casual Leave" },
                { "NumberOfDays", "2" },
                { "FromDate", DateTime.Today.ToString("dd-MMM-yyyy") },
                { "ToDate", DateTime.Today.AddDays(1).ToString("dd-MMM-yyyy") },
                { "ReportingManager", "Jane Doe" },
                { "ShiftName", "General Shift" },
                { "Reason", "Personal Work" }
            };
        }
    }
}