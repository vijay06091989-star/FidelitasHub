using FidelitasHub.Data;
using FidelitasHub.Models;
using FidelitasHub.Services.Email;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace FidelitasHub.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public AccountController(
            ApplicationDbContext context,
            EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        //=====================================================
        // LOGIN PAGE
        //=====================================================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        //=====================================================
        // PROCESS LOGIN
        //=====================================================

        [HttpPost]
        public IActionResult Login(string employeeCode, string password)
        {
            var employee = _context.Employees.FirstOrDefault(e =>
                e.EmployeeCode == employeeCode &&
                e.Password == password &&
                e.IsActive);

            if (employee == null)
            {
                ViewBag.Error = "Invalid Employee Code or Password.";
                return View();
            }

            // Store Session
            HttpContext.Session.SetString("EmployeeCode", employee.EmployeeCode);
            HttpContext.Session.SetString("EmployeeName", employee.EmployeeName);
            HttpContext.Session.SetString("Role", employee.Role);

            //=====================================================
            // FORCE PASSWORD CHANGE
            //=====================================================

            // Automatically expire password after 30 days
            if ((DateTime.Now - employee.PasswordLastChanged).TotalDays >= 30)
            {
                employee.MustChangePassword = true;
                _context.SaveChanges();
            }

            if (employee.MustChangePassword)
            {
                return RedirectToAction("ChangePassword");
            }

            //=====================================================
            // ROLE BASED REDIRECTION
            //=====================================================

            if (string.Equals(employee.Role, "Viewer", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(employee.Role, "Editor", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Productivity");
            }

            // Normal employee login now opens My Dashboard directly.
            return RedirectToAction("Dashboard", "Attendance");
        }

        //=====================================================
        // CHANGE PASSWORD PAGE
        //=====================================================

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("EmployeeCode")))
                return RedirectToAction("Login");

            return View();
        }

        //=====================================================
        // SAVE NEW PASSWORD
        //=====================================================

        [HttpPost]
        public IActionResult ChangePassword(string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                ViewBag.Error = "Password cannot be empty.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            //=====================================================
            // PASSWORD COMPLEXITY VALIDATION
            //=====================================================

            if (newPassword.Length < 8)
            {
                ViewBag.Error = "Password must be at least 8 characters long.";
                return View();
            }

            if (!Regex.IsMatch(newPassword, "[A-Z]"))
            {
                ViewBag.Error = "Password must contain at least one uppercase letter.";
                return View();
            }

            if (!Regex.IsMatch(newPassword, "[a-z]"))
            {
                ViewBag.Error = "Password must contain at least one lowercase letter.";
                return View();
            }

            if (!Regex.IsMatch(newPassword, "[0-9]"))
            {
                ViewBag.Error = "Password must contain at least one number.";
                return View();
            }

            if (!Regex.IsMatch(newPassword, @"[^a-zA-Z0-9]"))
            {
                ViewBag.Error = "Password must contain at least one special character.";
                return View();
            }

            var employeeCode = HttpContext.Session.GetString("EmployeeCode");

            var employee = _context.Employees.FirstOrDefault(e => e.EmployeeCode == employeeCode);

            if (employee == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login");
            }

            //=====================================================
            // PREVENT SAME PASSWORD
            //=====================================================

            if (employee.Password == newPassword)
            {
                ViewBag.Error = "Your new password must be different from your current password.";
                return View();
            }

            employee.Password = newPassword;
            employee.MustChangePassword = false;
            employee.PasswordLastChanged = DateTime.Now;

            _context.SaveChanges();

            TempData["Success"] = "Password changed successfully.";

            if (string.Equals(employee.Role, "Viewer", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(employee.Role, "Editor", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Productivity");
            }

            // After a mandatory password change, normal employees also
            // land directly on My Dashboard.
            return RedirectToAction("Dashboard", "Attendance");
        }

        //=====================================================
        // FORGOT PASSWORD
        //=====================================================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        //=====================================================
        // FORGOT PASSWORD
        //=====================================================

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string employeeCode)
        {
            if (string.IsNullOrWhiteSpace(employeeCode))
            {
                ViewBag.Error = "Please enter your Employee Code.";
                return View();
            }

            var employee = _context.Employees.FirstOrDefault(e =>
                e.EmployeeCode == employeeCode &&
                e.IsActive);

            if (employee == null)
            {
                ViewBag.Error = "Employee not found.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(employee.Email))
            {
                ViewBag.Error = "No email address is available for this employee. Please contact HR.";
                return View();
            }

            //=====================================================
            // Generate Temporary Password
            //=====================================================

            var temporaryPassword = "Temp@" + Random.Shared.Next(1000, 9999);

            try
            {
                //=====================================================
                // Send Email FIRST
                //=====================================================

                var values = new Dictionary<string, string>
                {
                    { "EmployeeName", employee.EmployeeName },
                    { "TemporaryPassword", temporaryPassword }
                };

                await _emailService.SendTemplateAsync(
                    "FORGOT_PASSWORD",
                    employee.Email,
                    values);

                //=====================================================
                // Save ONLY after successful email
                //=====================================================

                employee.Password = temporaryPassword;
                employee.MustChangePassword = true;
                employee.PasswordLastChanged = DateTime.Now;

                _context.SaveChanges();

                TempData["Success"] =
                    "A temporary password has been sent to your registered email address.";

                return RedirectToAction(nameof(ForgotPassword));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                ViewBag.Error =
                    "Unable to send the temporary password email. Please try again later.";

                return View();
            }
        }

        //=====================================================
        // LOGOUT
        //=====================================================

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }
    }
}
