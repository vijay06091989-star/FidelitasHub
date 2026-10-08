namespace FidelitasHub.Models
{
    public class ProductivitySetupViewModel
    {
        public List<ProductivityProcess> Processes { get; set; } = new();
        public List<ProductivityActivity> Activities { get; set; } = new();
        public List<ProductivityAssignment> Assignments { get; set; } = new();
        public List<Client> Clients { get; set; } = new();
        public List<Employee> Employees { get; set; } = new();
        public List<Department> Departments { get; set; } = new();
        public List<ProductivityGroup> Groups { get; set; } = new();
        public List<ProductivityGroupMember> GroupMembers { get; set; } = new();
        public List<ProductivityGroupAssignment> GroupAssignments { get; set; } = new();
        public int? SelectedProcessId { get; set; }
    }

    public class ProductivityRegisterViewModel
    {
        public DateTime ProductionDate { get; set; } = DateTime.Today;
        public int EmployeeId { get; set; }
        public int ClientId { get; set; }
        public int ProductivityActivityId { get; set; }
        public decimal Quantity { get; set; }
        public string? Remarks { get; set; }

        // The register now supports multiple activity rows for the same
        // employee/date and saves them together in one transaction.
        public List<ProductivityRegisterLineViewModel> Lines { get; set; } = new();

        public List<Employee> Employees { get; set; } = new();
        public List<Client> Clients { get; set; } = new();
        public List<ProductivityActivity> Activities { get; set; } = new();
        public List<ProductivityRegisterAssignmentOption> AssignmentOptions { get; set; } = new();
        public List<ProductivityEntry> RecentEntries { get; set; } = new();
    }

    public class ProductivityRegisterLineViewModel
    {
        public int ClientId { get; set; }
        public int ProductivityActivityId { get; set; }
        public decimal Quantity { get; set; }
        public string? Remarks { get; set; }
    }

    public class ProductivityRegisterAssignmentOption
    {
        public int ClientId { get; set; }
        public int ActivityId { get; set; }
        public string ClientCode { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public string ActivityName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal TargetPerDay { get; set; }
        public decimal Weight { get; set; }
    }

    public class ProductivityReportRow
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public int ClientId { get; set; }
        public int ActivityId { get; set; }
        public string ClientCode { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public string ActivityName { get; set; } = string.Empty;
        public decimal Target { get; set; }
        public decimal Actual { get; set; }
        public decimal WeightedAchievement { get; set; }
        public decimal AchievementPercent => Target <= 0 ? 0 : (Actual / Target) * 100m;
    }

    public class ProductivityReportsViewModel
    {
        public DateTime FromDate { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        public DateTime ToDate { get; set; } = DateTime.Today;
        public int? EmployeeId { get; set; }
        public int? ClientId { get; set; }
        public int? ProcessId { get; set; }
        public List<Employee> Employees { get; set; } = new();
        public List<Client> Clients { get; set; } = new();
        public List<ProductivityProcess> Processes { get; set; } = new();
        public List<ProductivityReportRow> Rows { get; set; } = new();
    }
}
