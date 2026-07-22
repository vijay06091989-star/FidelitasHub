using Microsoft.AspNetCore.Mvc;

namespace FidelitasHub.Controllers
{
    public class ReportsController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Title = "Reports";
            ViewBag.SubTitle = "Reports & Analytics";
            ViewBag.Icon = "fa-solid fa-chart-column";

            ViewBag.Description =
                "Attendance reports, leave reports, late arrival analysis, employee summaries, department-wise reports, Excel exports and management dashboards will be available in the next update.";

            return View("~/Views/Shared/ComingSoon.cshtml");
        }
    }
}