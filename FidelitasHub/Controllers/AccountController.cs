using FidelitasHub.Data;
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
            // ROLE BASED REDIRECTION
            //=====================================================

            switch (employee.Role)
            {
                case "Admin":
                case "SuperAdmin":
                case "HR":
                case "Manager":
                case "TeamLeader":
                default:
                    return RedirectToAction("Index", "Home");
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