using FidelitasHub.Data;
using FidelitasHub.Models;
using Microsoft.AspNetCore.Mvc;

namespace FidelitasHub.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
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

            if (employee.MustChangePassword)
            {
                return RedirectToAction("ChangePassword");
            }

            //=====================================================
            // ROLE BASED REDIRECTION
            //=====================================================

            return RedirectToAction("Index", "Home");
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

            var employeeCode = HttpContext.Session.GetString("EmployeeCode");

            var employee = _context.Employees.FirstOrDefault(e => e.EmployeeCode == employeeCode);

            if (employee == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login");
            }

            employee.Password = newPassword;
            employee.MustChangePassword = false;

            _context.SaveChanges();

            TempData["Success"] = "Password changed successfully.";

            return RedirectToAction("Index", "Home");
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