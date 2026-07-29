namespace FidelitasHub.Services.Email.Builders
{
    public static class EmployeeEmailData
    {
        public static Dictionary<string, string> Create(
            string employeeName,
            string employeeCode,
            string department,
            string designation,
            string reportingManager,
            string companyName)
        {
            return new Dictionary<string, string>
            {
                { "EmployeeName", employeeName },
                { "EmployeeCode", employeeCode },
                { "Department", department },
                { "Designation", designation },
                { "ReportingManager", reportingManager },
                { "CompanyName", companyName }
            };
        }
    }
}