using FidelitasHub.Data;
using FidelitasHub.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace FidelitasHub.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Login", "Account");
            }

            if (role != "Admin" && role != "SuperAdmin")
            {
                return RedirectToAction("Index", "Home");
            }

            var today = DateTimeHelper.GetIST().Date;

            ViewBag.EmployeeName = HttpContext.Session.GetString("EmployeeName");
            ViewBag.TotalEmployees = _context.Employees.Count();
            ViewBag.ActiveEmployees = _context.Employees.Count(e => e.IsActive);
            ViewBag.PresentToday = _context.Attendances
                .Count(a => a.AttendanceDate.Date == today && a.PunchIn != null);

            return View();
        }
    }
}
