using FidelitasHub.Data;
using FidelitasHub.Helpers;
using FidelitasHub.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FidelitasHub.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var employeeCode = HttpContext.Session.GetString("EmployeeCode");

            if (string.IsNullOrEmpty(employeeCode))
            {
                return RedirectToAction("Login", "Account");
            }

            var employee = _context.Employees
                .FirstOrDefault(e => e.EmployeeCode == employeeCode);

            if (employee == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var shift = _context.Shifts
                .FirstOrDefault(s => s.ShiftId == employee.ShiftId);

            var today = DateTimeHelper.GetIST().Date;

            var attendance = _context.Attendances
                .FirstOrDefault(a =>
                    a.EmployeeId == employee.EmployeeId &&
                    a.AttendanceDate.Date == today);

            DashboardViewModel model = new DashboardViewModel
            {
                EmployeeName = employee.EmployeeName,
                ShiftName = shift != null ? shift.ShiftName : "Not Assigned",
                Status = "Not Punched In",

                CanPunchIn = true,
                CanBreak = false,
                CanResume = false,
                CanPunchOut = false
            };

            if (attendance != null)
            {
                model.Status = attendance.Status;
                model.PunchIn = attendance.PunchIn;
                model.PunchOut = attendance.PunchOut;
                model.TotalBreakMinutes = attendance.TotalBreakMinutes;

                if (attendance.PunchIn.HasValue)
                {
                    model.WorkedMinutes =
                        (DateTimeHelper.GetIST() - attendance.PunchIn.Value).TotalMinutes;
                }

                switch (attendance.Status)
                {
                    case "Working":

                        model.CanPunchIn = false;
                        model.CanBreak = true;
                        model.CanResume = false;
                        model.CanPunchOut = true;

                        break;

                    case "On Break":

                        model.CanPunchIn = false;
                        model.CanBreak = false;
                        model.CanResume = true;
                        model.CanPunchOut = false;

                        break;

                    case "Punched Out":

                        model.CanPunchIn = false;
                        model.CanBreak = false;
                        model.CanResume = false;
                        model.CanPunchOut = false;

                        break;

                    default:

                        model.CanPunchIn = true;
                        model.CanBreak = false;
                        model.CanResume = false;
                        model.CanPunchOut = false;

                        break;
                }
            }

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}