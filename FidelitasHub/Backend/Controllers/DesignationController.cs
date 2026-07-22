using Microsoft.AspNetCore.Mvc;

namespace FidelitasHub.Controllers
{
    public class DesignationController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Title = "Designations";
            ViewBag.SubTitle = "Designation Management";
            ViewBag.Icon = "fa-solid fa-user-tie";

            ViewBag.Description =
                "Create and maintain employee designations, reporting hierarchy, job titles and organizational structure. This feature will be available in the next update.";

            return View("~/Views/Shared/ComingSoon.cshtml");
        }
    }
}