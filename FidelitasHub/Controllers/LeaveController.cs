using FidelitasHub.Data;
using FidelitasHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FidelitasHub.Controllers
{
    public class LeaveController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LeaveController(ApplicationDbContext context)
        {
            _context = context;
        }

        //==================================================
        // Leave Application
        //==================================================

        [HttpGet]
        public IActionResult ApplyLeave()
        {
            LeaveApplicationViewModel model = new();

            model.Employees = _context.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.EmployeeCode)
                .Select(e => new SelectListItem
                {
                    Value = e.EmployeeId.ToString(),
                    Text = e.EmployeeCode + " - " + e.EmployeeName
                })
                .ToList();

            model.LeaveTypes = new List<SelectListItem>()
            {
                new SelectListItem{ Value="CL", Text="Casual / Sick Leave"},
                new SelectListItem{ Value="LOP", Text="Loss of Pay"},
                new SelectListItem{ Value="CO", Text="Comp Off"},
                new SelectListItem{ Value="ML", Text="Maternity Leave"}
            };

            return View(model);
        }
    }
}