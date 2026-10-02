using FidelitasHub.Data;
using FidelitasHub.Models;
using FidelitasHub.Helpers;
using FidelitasHub.Services.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FidelitasHub.Controllers
{
    public class EmployeeDocumentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IProtectedActionService _protectedActionService;
        private readonly IWebHostEnvironment _environment;

        private const long MaxFileSize = 10 * 1024 * 1024;

        public EmployeeDocumentsController(
            ApplicationDbContext context,
            IProtectedActionService protectedActionService,
            IWebHostEnvironment environment)
        {
            _context = context;
            _protectedActionService = protectedActionService;
            _environment = environment;
        }

        //==================================================
        // MY DASHBOARD PROFILE + DOCUMENT STATUS
        //==================================================

        [HttpGet]
        public IActionResult DashboardData()
        {
            var employee = GetLoggedInEmployee();

            if (employee == null)
                return Unauthorized();

            var shiftName = employee.ShiftId.HasValue
                ? _context.Shifts
                    .Where(s => s.ShiftId == employee.ShiftId.Value)
                    .Select(s => s.ShiftName)
                    .FirstOrDefault()
                : null;

            var data = new
            {
                employeeCode = employee.EmployeeCode,
                employeeName = employee.EmployeeName,
                department = employee.Department,
                designation = employee.Designation,
                email = employee.Email,
                mobile = employee.Mobile,
                role = employee.Role,
                shift = shiftName ?? "Not Assigned",
                dateJoined = employee.DateJoined.ToString("dd-MMM-yyyy"),
                reportingManager = employee.ReportingManagerId.HasValue
                    ? _context.Employees
                        .Where(e => e.EmployeeId == employee.ReportingManagerId.Value)
                        .Select(e => e.EmployeeName)
                        .FirstOrDefault()
                    : null,
                reportingTeamLeader = employee.ReportingTeamLeaderId.HasValue
                    ? _context.Employees
                        .Where(e => e.EmployeeId == employee.ReportingTeamLeaderId.Value)
                        .Select(e => e.EmployeeName)
                        .FirstOrDefault()
                    : null,
                gender = employee.Gender,
                maritalStatus = employee.MaritalStatus,
                dateOfBirth = employee.DateOfBirth?.ToString("dd-MMM-yyyy"),
                bloodGroup = employee.BloodGroup,
                panNumber = employee.PANNumber,
                aadharNumber = employee.AadharNumber,
                fatherName = employee.FatherName,
                emergencyContactPerson = employee.EmergencyContactPerson,
                emergencyContactNumber = employee.EmergencyContactNumber,
                permanentAddress = employee.PermanentAddress,
                residentialAddress = employee.ResidentialAddress,
                personalEmail = employee.PersonalEmail,
                aadhaarUploaded = HasDocument(employee.EmployeeId, EmployeeDocumentStorage.Aadhaar),
                panUploaded = HasDocument(employee.EmployeeId, EmployeeDocumentStorage.Pan)
            };

            return Json(data);
        }

        //==================================================
        // MY DOCUMENT UPLOAD
        //==================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaxFileSize)]
        public async Task<IActionResult> Upload(
            string documentType,
            IFormFile? file)
        {
            var employee = GetLoggedInEmployee();

            if (employee == null)
                return RedirectToAction("Login", "Account");

            var type = EmployeeDocumentStorage.NormalizeType(documentType);

            if (string.IsNullOrEmpty(type))
            {
                TempData["Error"] = "Invalid document type.";
                return RedirectToAction("Dashboard", "Attendance");
            }

            if (file == null || file.Length == 0)
            {
                TempData["Error"] =
                    $"Please select your {EmployeeDocumentStorage.GetDisplayName(type)} file.";
                return RedirectToAction("Dashboard", "Attendance");
            }

            if (file.Length > MaxFileSize)
            {
                TempData["Error"] = "The document must be 10 MB or smaller.";
                return RedirectToAction("Dashboard", "Attendance");
            }

            var extension = Path.GetExtension(file.FileName);
            if (!EmployeeDocumentStorage.IsAllowedExtension(extension))
            {
                TempData["Error"] =
                    "Only PDF, JPG, JPEG and PNG files are allowed.";
                return RedirectToAction("Dashboard", "Attendance");
            }

            try
            {
                await EmployeeDocumentStorage.SaveAsync(
                    _environment.ContentRootPath,
                    employee.EmployeeId,
                    type,
                    file);

                TempData["Success"] =
                    $"{EmployeeDocumentStorage.GetDisplayName(type)} uploaded successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    "The document could not be uploaded. " + ex.Message;
            }

            return RedirectToAction("Dashboard", "Attendance");
        }

        //==================================================
        // MY DOCUMENT VIEW / DOWNLOAD
        //==================================================

        [HttpGet]
        public IActionResult ViewMyDocument(string documentType)
        {
            var employee = GetLoggedInEmployee();
            if (employee == null)
                return RedirectToAction("Login", "Account");

            return ServeDocument(
                employee.EmployeeId,
                documentType,
                download: false);
        }

        [HttpGet]
        public IActionResult DownloadMyDocument(string documentType)
        {
            var employee = GetLoggedInEmployee();
            if (employee == null)
                return RedirectToAction("Login", "Account");

            return ServeDocument(
                employee.EmployeeId,
                documentType,
                download: true);
        }

        //==================================================
        // EMPLOYEE MASTER - DOCUMENT STATUS
        //==================================================

        [HttpGet]
        public IActionResult Status(int employeeId)
        {
            var employee = _context.Employees.Find(employeeId);
            if (employee == null)
                return NotFound();

            if (!IsEditAuthorized(employeeId))
                return Unauthorized();

            return Json(new
            {
                aadhaarUploaded = HasDocument(employeeId, EmployeeDocumentStorage.Aadhaar),
                panUploaded = HasDocument(employeeId, EmployeeDocumentStorage.Pan)
            });
        }

        //==================================================
        // EMPLOYEE MASTER - DOCUMENT VIEW / DOWNLOAD
        //==================================================

        [HttpGet]
        public IActionResult ViewDocument(
            int employeeId,
            string documentType)
        {
            if (_context.Employees.Find(employeeId) == null)
                return NotFound();

            if (!IsEditAuthorized(employeeId))
            {
                return RedirectToAction(
                    "VerifyPin",
                    "Employee",
                    new
                    {
                        id = employeeId,
                        returnUrl = Url.Action(
                            nameof(ViewDocument),
                            new { employeeId, documentType })
                    });
            }

            return ServeDocument(
                employeeId,
                documentType,
                download: false);
        }

        [HttpGet]
        public IActionResult DownloadDocument(
            int employeeId,
            string documentType)
        {
            if (_context.Employees.Find(employeeId) == null)
                return NotFound();

            if (!IsEditAuthorized(employeeId))
            {
                return RedirectToAction(
                    "VerifyPin",
                    "Employee",
                    new
                    {
                        id = employeeId,
                        returnUrl = Url.Action(
                            nameof(DownloadDocument),
                            new { employeeId, documentType })
                    });
            }

            return ServeDocument(
                employeeId,
                documentType,
                download: true);
        }

        private Employee? GetLoggedInEmployee()
        {
            var employeeCode =
                HttpContext.Session.GetString("EmployeeCode");

            if (string.IsNullOrWhiteSpace(employeeCode))
                return null;

            return _context.Employees
                .FirstOrDefault(e => e.EmployeeCode == employeeCode);
        }

        private bool IsEditAuthorized(int employeeId)
        {
            return _protectedActionService.IsAuthorized(
                HttpContext,
                ProtectedActionService.EmployeeEditAction,
                employeeId);
        }

        private bool HasDocument(int employeeId, string documentType)
        {
            return EmployeeDocumentStorage.FindExistingFile(
                _environment.ContentRootPath,
                employeeId,
                documentType) != null;
        }

        private IActionResult ServeDocument(
            int employeeId,
            string documentType,
            bool download)
        {
            var type = EmployeeDocumentStorage.NormalizeType(documentType);

            if (string.IsNullOrEmpty(type))
                return BadRequest("Invalid document type.");

            var path = EmployeeDocumentStorage.FindExistingFile(
                _environment.ContentRootPath,
                employeeId,
                type);

            if (path == null || !System.IO.File.Exists(path))
                return NotFound("Document has not been uploaded.");

            var contentType =
                EmployeeDocumentStorage.GetContentType(path);

            if (download)
            {
                return PhysicalFile(
                    path,
                    contentType,
                    Path.GetFileName(path));
            }

            return PhysicalFile(
                path,
                contentType,
                enableRangeProcessing: true);
        }
    }
}
