using Microsoft.AspNetCore.Mvc;

namespace FidelitasHub.Controllers
{
    public class AdminController : Controller
    {
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

            ViewBag.EmployeeName = HttpContext.Session.GetString("EmployeeName");

            return View();
        }
    }
}