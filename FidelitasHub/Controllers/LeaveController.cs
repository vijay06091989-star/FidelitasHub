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

            // Logged-in Employee
            string? employeeCode = HttpContext.Session.GetString("EmployeeCode");

            if (!string.IsNullOrWhiteSpace(employeeCode))
            {
                var employee = _context.Employees
                    .FirstOrDefault(e => e.EmployeeCode == employeeCode);

                if (employee != null)
                {
                    model.EmployeeId = employee.EmployeeId;

                    var leaveBalance = _context.EmployeeLeaveBalances
                        .FirstOrDefault(x => x.EmployeeId == employee.EmployeeId);

                    if (leaveBalance != null)
                    {
                        model.LeaveBalance = leaveBalance.CurrentLeaveBalance;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(employeeCode))
            {
                var employee = _context.Employees
                    .FirstOrDefault(e => e.EmployeeCode == employeeCode);

                if (employee != null)
                {
                    model.Employees = new List<SelectListItem>
        {
            new SelectListItem
            {
                Value = employee.EmployeeId.ToString(),
                Text = employee.EmployeeCode + " - " + employee.EmployeeName
            }
        };
                }
            }

            // model.LeaveTypes = new List<SelectListItem>()
            // {
            //     new SelectListItem{ Value="CL", Text="Casual / Sick Leave"},
            //     new SelectListItem{ Value="LOP", Text="Loss of Pay"},
            //     new SelectListItem{ Value="CO", Text="Comp Off"},
            //     new SelectListItem{ Value="ML", Text="Maternity Leave"}
            // };   

            return View(model);
        }
    }
}