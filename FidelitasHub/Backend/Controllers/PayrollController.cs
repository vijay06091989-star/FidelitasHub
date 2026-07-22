using Microsoft.AspNetCore.Mvc;

namespace FidelitasHub.Controllers
{
    public class PayrollController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Title = "Payroll Calendar";
            ViewBag.SubTitle = "Payroll Processing Calendar";
            ViewBag.Icon = "fa-solid fa-money-check-dollar";

            ViewBag.Description =
                "Configure payroll processing dates, salary release schedules, payroll periods and statutory payroll timelines. This feature will be available in the next update.";

            return View("~/Views/Shared/ComingSoon.cshtml");
        }
    }
}