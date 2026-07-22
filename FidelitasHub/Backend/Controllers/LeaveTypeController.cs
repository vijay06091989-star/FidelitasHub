using Microsoft.AspNetCore.Mvc;

namespace FidelitasHub.Controllers
{
    public class LeaveTypeController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Title = "Leave Types";
            ViewBag.SubTitle = "Leave Type Management";
            ViewBag.Icon = "fa-solid fa-plane-departure";

            ViewBag.Description =
                "Configure Casual Leave (CL), Sick Leave (SL), Earned Leave (EL), Loss of Pay (LOP), Comp-Off, Maternity Leave and other leave policies. This feature will be available in the next update.";

            return View("~/Views/Shared/ComingSoon.cshtml");
        }
    }
}