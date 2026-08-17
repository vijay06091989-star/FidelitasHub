using FidelitasHub.Data;
using FidelitasHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FidelitasHub.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmployeeController(ApplicationDbContext context)
        {
            _context = context;
        }

        //==================================================
        // Employee List / Search
        //==================================================

        public IActionResult Index(string searchText, string status)
        {
            var employees = _context.Employees.AsQueryable();

            //=========================
            // Search
            //=========================

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                employees = employees.Where(e =>

                    e.EmployeeCode.Contains(searchText) ||

                    e.EmployeeName.Contains(searchText) ||

                    e.Department.Contains(searchText) ||

                    e.Designation.Contains(searchText) ||

                    e.Email.Contains(searchText)

                );
            }

            //=========================
            // Status Filter
            //=========================

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status == "Active")
                {
                    employees = employees.Where(e => e.IsActive);
                }
                else if (status == "Inactive")
                {
                    employees = employees.Where(e => !e.IsActive);
                }
            }

            ViewBag.SearchText = searchText;
            ViewBag.Status = status;

            ViewBag.TotalEmployees =
                _context.Employees.Count();

            ViewBag.ActiveEmployees =
                _context.Employees.Count(e => e.IsActive);

            ViewBag.InactiveEmployees =
                _context.Employees.Count(e => !e.IsActive);

            // Load Shift Names
            ViewBag.ShiftNames = _context.Shifts
                                         .ToDictionary(
                                             s => s.ShiftId,
                                             s => s.ShiftName);

            return View(employees
                .OrderBy(e => e.EmployeeCode)
                .ToList());
        }

        //==================================================
        // Add Employee (GET)
        //==================================================

        [HttpGet]
        public IActionResult Create()
        {
            LoadDepartments();

            LoadShifts();

            LoadReportingEmployees();

            return View();
        }

        //==================================================
        // Add Employee (POST)
        //==================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Employee employee)
        {
            employee.Password = "Welcome@123";
            employee.IsActive = true;

            ModelState.Remove(nameof(Employee.Password));

            //--------------------------------------------------
            // Duplicate Employee Code
            //--------------------------------------------------

            if (_context.Employees.Any(e =>
                e.EmployeeCode.Trim().ToUpper() ==
                employee.EmployeeCode.Trim().ToUpper()))
            {
                ModelState.AddModelError(
                    "EmployeeCode",
                    "Employee Code already exists.");
            }

            //--------------------------------------------------
            // Duplicate Email
            //--------------------------------------------------

            if (!string.IsNullOrWhiteSpace(employee.Email))
            {
                if (_context.Employees.Any(e =>
                    e.Email.Trim().ToUpper() ==
                    employee.Email.Trim().ToUpper()))
                {
                    ModelState.AddModelError(
                        "Email",
                        "Email Address already exists.");
                }
            }

            if (ModelState.IsValid)
            {
                _context.Employees.Add(employee);

                _context.SaveChanges();

                TempData["Success"] =
                    "Employee created successfully.";

                return RedirectToAction(nameof(Index));
            }

            LoadDepartments();

            LoadShifts();

            LoadReportingEmployees(
                employee.ReportingManagerId,
                employee.ReportingTeamLeaderId);

            return View(employee);
        }

        //==================================================
        // Edit Employee (GET)
        //==================================================

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var employee = _context.Employees.Find(id);

            if (employee == null)
            {
                return NotFound();
            }

            LoadDepartments();

            LoadShifts();

            LoadReportingEmployees(
                employee.ReportingManagerId,
                employee.ReportingTeamLeaderId,
                employee.EmployeeId);

            return View(employee);
        }

        //==================================================
        // Edit Employee (POST)
        //==================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Employee employee)
        {
            ModelState.Remove(nameof(Employee.Password));

            //--------------------------------------------------
            // Prevent Employee from reporting to themselves
            //--------------------------------------------------

            if (employee.ReportingManagerId == employee.EmployeeId)
            {
                ModelState.AddModelError(
                    "ReportingManagerId",
                    "An employee cannot be their own Reporting Manager.");
            }

            if (employee.ReportingTeamLeaderId == employee.EmployeeId)
            {
                ModelState.AddModelError(
                    "ReportingTeamLeaderId",
                    "An employee cannot be their own Reporting Team Leader.");
            }

            //--------------------------------------------------
            // Duplicate Employee Code
            //--------------------------------------------------

            if (_context.Employees.Any(e =>
                e.EmployeeId != employee.EmployeeId &&
                e.EmployeeCode.Trim().ToUpper() ==
                employee.EmployeeCode.Trim().ToUpper()))
            {
                ModelState.AddModelError(
                    "EmployeeCode",
                    "Employee Code already exists.");
            }

            //--------------------------------------------------
            // Duplicate Email
            //--------------------------------------------------

            if (!string.IsNullOrWhiteSpace(employee.Email))
            {
                if (_context.Employees.Any(e =>
                    e.EmployeeId != employee.EmployeeId &&
                    e.Email.Trim().ToUpper() ==
                    employee.Email.Trim().ToUpper()))
                {
                    ModelState.AddModelError(
                        "Email",
                        "Email Address already exists.");
                }
            }

            if (ModelState.IsValid)
            {
                _context.Update(employee);

                _context.SaveChanges();

                TempData["Success"] =
                    "Employee updated successfully.";

                return RedirectToAction(nameof(Index));
            }

            LoadDepartments();

            LoadShifts();

            LoadReportingEmployees(
                employee.ReportingManagerId,
                employee.ReportingTeamLeaderId,
                employee.EmployeeId);

            return View(employee);
        }

        //==================================================
        // Disable Employee
        //==================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Disable(int id)
        {
            var employee = _context.Employees.Find(id);

            if (employee == null)
            {
                return NotFound();
            }

            employee.IsActive = false;

            _context.SaveChanges();

            TempData["Success"] = "Employee disabled successfully.";

            return RedirectToAction(nameof(Index));
        }

        //==================================================
        // Enable Employee
        //==================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Enable(int id)
        {
            var employee = _context.Employees.Find(id);

            if (employee == null)
            {
                return NotFound();
            }

            employee.IsActive = true;

            _context.SaveChanges();

            TempData["Success"] = "Employee enabled successfully.";

            return RedirectToAction(nameof(Index));
        }

        //==================================================
        // Load Department Dropdown
        //==================================================

        private void LoadDepartments()
        {
            ViewBag.Departments = new SelectList(

                _context.Departments
                        .Where(d => d.IsActive)
                        .OrderBy(d => d.DepartmentName)
                        .ToList(),

                "DepartmentName",

                "DepartmentName"

            );
        }

        //==================================================
        // Load Shift Dropdown
        //==================================================

        private void LoadShifts()
        {
            ViewBag.Shifts = new SelectList(

                _context.Shifts
                        .Where(s => s.IsActive)
                        .OrderBy(s => s.ShiftName)
                        .ToList(),

                "ShiftId",

                "ShiftName"

            );
        }

        //==================================================
        // Load Reporting Manager / Team Leader Dropdowns
        //==================================================

        private void LoadReportingEmployees(
    int? selectedManagerId = null,
    int? selectedTeamLeaderId = null,
    int? excludeEmployeeId = null)
        {
            //--------------------------------------------------
            // Reporting Managers
            // Only Active Managers and Admins
            //--------------------------------------------------

            var managers = _context.Employees
                .Where(e =>
                    e.IsActive &&
                    (e.Role == "Manager" || e.Role == "Admin"));

            //--------------------------------------------------
            // Reporting Team Leaders
            // Only Active Team Leaders
            //--------------------------------------------------

            var teamLeaders = _context.Employees
                .Where(e =>
                    e.IsActive &&
                    e.Role == "Team Leader");

            //--------------------------------------------------
            // Exclude the employee being edited
            //--------------------------------------------------

            if (excludeEmployeeId.HasValue)
            {
                managers = managers.Where(e =>
                    e.EmployeeId != excludeEmployeeId.Value);

                teamLeaders = teamLeaders.Where(e =>
                    e.EmployeeId != excludeEmployeeId.Value);
            }

            //--------------------------------------------------
            // Build Manager List
            //--------------------------------------------------

            var managerList = managers
                .OrderBy(e => e.EmployeeName)
                .ToList();

            //--------------------------------------------------
            // Build Team Leader List
            //--------------------------------------------------

            var teamLeaderList = teamLeaders
                .OrderBy(e => e.EmployeeName)
                .ToList();

            //--------------------------------------------------
            // Reporting Manager Dropdown
            //--------------------------------------------------

            ViewBag.ReportingManagers = new SelectList(
                managerList,
                "EmployeeId",
                "EmployeeName",
                selectedManagerId);

            //--------------------------------------------------
            // Reporting Team Leader Dropdown
            //--------------------------------------------------

            ViewBag.ReportingTeamLeaders = new SelectList(
                teamLeaderList,
                "EmployeeId",
                "EmployeeName",
                selectedTeamLeaderId);
        }
    }
}