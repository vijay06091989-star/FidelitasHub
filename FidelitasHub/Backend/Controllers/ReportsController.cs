using FidelitasHub.Data;
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

        public IActionResult Attendance()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");
            return View();
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
