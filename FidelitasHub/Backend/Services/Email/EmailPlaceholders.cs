namespace FidelitasHub.Services.Email
{
    public static class EmailPlaceholders
    {
        public static readonly List<string> Common = new()
        {
            "EmployeeName",
            "EmployeeCode",
            "Department",
            "Designation",
            "ReportingManager",
            "CompanyName"
        };

        public static readonly List<string> Leave = new()
        {
            "LeaveType",
            "FromDate",
            "ToDate",
            "NumberOfDays",
            "Reason"
        };

        public static readonly List<string> Attendance = new()
        {
            "AttendanceDate",
            "PunchIn",
            "PunchOut",
            "ShiftName"
        };

        public static List<string> GetAll()
        {
            return Common
                .Concat(Leave)
                .Concat(Attendance)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
        }
    }
}