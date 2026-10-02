using FidelitasHub.Data;
using FidelitasHub.Helpers;
using FidelitasHub.Models;
using FidelitasHub.Services.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FidelitasHub.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
    private readonly IProtectedActionService _protectedActionService;

        public EmployeeController(
        ApplicationDbContext context,
        IProtectedActionService protectedActionService)
        {
            _context = context;
        _protectedActionService = protectedActionService;
        }

        //==================================================
        // Employee List / Search
        //==================================================

        public IActionResult Index(string searchText, string status)
        {
            // Clear any employee-edit authorization left from a previous edit session.
            // This ensures returning to Employee Master requires PIN verification again.
            _protectedActionService.RevokeAll(
                HttpContext,
                ProtectedActionService.EmployeeEditAction);

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

            if (employee.Role == "SuperAdmin")
            {
                employee.ShiftId = null;
                employee.ReportingManagerId = null;
                employee.ReportingTeamLeaderId = null;
            }

            if (IsProductivityOnlyRole(employee.Role))
            {
                employee.ShiftId = null;
                employee.ReportingManagerId = null;
                employee.ReportingTeamLeaderId = null;
                employee.EnableIdleMonitoring = false;
            }


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

            if (!_protectedActionService.IsAuthorized(
                HttpContext,
                ProtectedActionService.EmployeeEditAction,
                id))
            {
                return RedirectToAction(
                    nameof(VerifyPin),
                    new
                    {
                        id
                    });
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
            if (!_protectedActionService.IsAuthorized(
                HttpContext,
                ProtectedActionService.EmployeeEditAction,
                employee.EmployeeId))
            {
                return RedirectToAction(
                    nameof(VerifyPin),
                    new
                    {
                        id = employee.EmployeeId,
                        returnUrl = Url.Action(
                            nameof(Edit),
                            new { id = employee.EmployeeId })
                    });
            }

            ModelState.Remove(nameof(Employee.Password));

            if (employee.Role == "SuperAdmin")
            {
                employee.ShiftId = null;
                employee.ReportingManagerId = null;
                employee.ReportingTeamLeaderId = null;
            }

            if (IsProductivityOnlyRole(employee.Role))
            {
                employee.ShiftId = null;
                employee.ReportingManagerId = null;
                employee.ReportingTeamLeaderId = null;
                employee.EnableIdleMonitoring = false;
            }

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
                if (!employee.EnableIdleMonitoring)
                {
                    var openIdleSessions = _context.EmployeeIdleSessions
                        .Where(i => i.EmployeeId == employee.EmployeeId && i.IdleEnd == null)
                        .ToList();

                    var now = DateTimeHelper.GetIST();

                    foreach (var idleSession in openIdleSessions)
                    {
                        idleSession.IdleEnd = now;
                        idleSession.DurationSeconds = Math.Max(
                            0,
                            (int)Math.Round((now - idleSession.IdleStart).TotalSeconds));
                    }
                }

                _context.Update(employee);

                _context.SaveChanges();
                _protectedActionService.Revoke(
                    HttpContext,
                    ProtectedActionService.EmployeeEditAction,
                    employee.EmployeeId);


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
        //==================================================
        // Employee Edit PIN Verification
        //==================================================

        [HttpGet]
        public IActionResult VerifyPin(int id, string? returnUrl = null)
        {
            var employee = _context.Employees.Find(id);

            if (employee == null)
                return NotFound();

            ViewBag.EmployeeName = employee.EmployeeName;
            ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl)
                ? Url.Action(nameof(Edit), new { id })
                : returnUrl;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyPin(
            int id,
            string pin,
            string? returnUrl = null)
        {
            var employee = _context.Employees.Find(id);

            if (employee == null)
                return NotFound();

            if (!_protectedActionService.VerifyPin(pin))
            {
                ViewBag.EmployeeName = employee.EmployeeName;
                ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl)
                    ? Url.Action(nameof(Edit), new { id })
                    : returnUrl;
                ViewBag.PinError = "Incorrect PIN. Please try again.";
                return View();
            }

            _protectedActionService.Grant(
                HttpContext,
                ProtectedActionService.EmployeeEditAction,
                id);

            return LocalRedirect(
                string.IsNullOrWhiteSpace(returnUrl)
                    ? Url.Action(nameof(Edit), new { id })!
                    : returnUrl);
        }

        //==================================================
        // Employee Edit PIN Verification
        //==================================================

        [HttpGet]
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
                    (e.Role == "Manager" ||
                     e.Role == "Admin" ||
                     e.Role == "SuperAdmin"));

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
        //==================================================
        // Productivity-only role helper
        //==================================================

        private static bool IsProductivityOnlyRole(string? role)
        {
            return string.Equals(role, "Viewer", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(role, "Editor", StringComparison.OrdinalIgnoreCase);
        }

    }
}




