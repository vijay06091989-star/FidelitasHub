using Microsoft.AspNetCore.Mvc;

namespace FidelitasHub.Controllers
{
    public class AdministrationController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Title = "Administration";
            ViewBag.SubTitle = "System Administration";
            ViewBag.Icon = "fa-solid fa-gears";

            ViewBag.Description =
                "System settings, email configuration, user roles, permissions, audit logs, application preferences, backup management and other administrative tools will be available in the next update.";

            return View("~/Views/Shared/ComingSoon.cshtml");
        }
    }
}