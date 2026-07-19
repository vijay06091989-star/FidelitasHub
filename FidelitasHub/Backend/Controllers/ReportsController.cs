using FidelitasHub.Data;
using FidelitasHub.Models;
using Microsoft.AspNetCore.Mvc;

namespace FidelitasHub.Controllers
{
    // Report screens. Filter UI is complete; result generation for the
    // attendance report is scaffolded for a later data pass.
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Admin" || role == "SuperAdmin";
        }

        public IActionResult Index()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");
            return View();
        }

        public IActionResult Attendance(DateTime? fromDate, DateTime? toDate, string shift)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var from = (fromDate ?? DateTime.Today.AddDays(-30)).Date;
            var to = (toDate ?? DateTime.Today).Date;

            var records = _context.Attendances
                .Where(a => a.AttendanceDate.Date >= from && a.AttendanceDate.Date <= to)
                .ToList();

            var employees = _context.Employees.ToList();
            var shiftNames = _context.Shifts.ToDictionary(s => s.ShiftId, s => s.ShiftName);

            // Optional shift filter (via each employee's assigned shift)
            if (!string.IsNullOrWhiteSpace(shift) && shift != "All")
            {
                var empIds = employees
                    .Where(e => shiftNames.TryGetValue(e.ShiftId, out var sn) && sn == shift)
                    .Select(e => e.EmployeeId)
                    .ToHashSet();

                records = records.Where(r => empIds.Contains(r.EmployeeId)).ToList();
            }

            var rows = records
                .GroupBy(r => r.EmployeeId)
                .Select(g =>
                {
                    var emp = employees.FirstOrDefault(e => e.EmployeeId == g.Key);
                    return new AttendanceReportRow
                    {
                        EmployeeCode = emp?.EmployeeCode ?? ("#" + g.Key),
                        EmployeeName = emp?.EmployeeName ?? "",
                        Department = emp?.Department ?? "",
                        PresentDays = g.Count(x => x.Status != "Absent"),
                        LateDays = g.Count(x => (x.Status ?? "").Contains("Late")),
                        HalfDays = g.Count(x => x.Status == "Half Day"),
                        AbsentDays = g.Count(x => x.Status == "Absent"),
                        WorkedHours = Math.Round(g.Sum(x => x.WorkedMinutes) / 60.0, 1)
                    };
                })
                .OrderBy(r => r.EmployeeCode)
                .ToList();

            ViewBag.FromDate = from;
            ViewBag.ToDate = to;
            ViewBag.Shift = shift ?? "All";
            ViewBag.HasFilter = fromDate.HasValue || toDate.HasValue || !string.IsNullOrWhiteSpace(shift);

            return View(rows);
        }

        public IActionResult Employee(string department, string status)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var employees = _context.Employees.AsQueryable();

            if (!string.IsNullOrWhiteSpace(department))
                employees = employees.Where(e => e.Department == department);

            if (status == "Active")
                employees = employees.Where(e => e.IsActive);
            else if (status == "Inactive")
                employees = employees.Where(e => !e.IsActive);

            ViewBag.Departments = _context.Departments
                .OrderBy(d => d.DepartmentName)
                .Select(d => d.DepartmentName)
                .ToList();
            ViewBag.Department = department;
            ViewBag.Status = status;

            return View(employees.OrderBy(e => e.EmployeeCode).ToList());
        }

        public IActionResult Leave(string status)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var leaves = _context.LeaveApplications.AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                leaves = leaves.Where(l => l.Status == status);

            var employeeNames = _context.Employees
                .ToDictionary(e => e.EmployeeId, e => e.EmployeeName);
            ViewBag.EmployeeNames = employeeNames;
            ViewBag.Status = status;

            return View(leaves.OrderByDescending(l => l.AppliedOn).ToList());
        }
    }
}
