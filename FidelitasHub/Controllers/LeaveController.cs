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
        // Apply Leave - GET
        //==================================================
        [HttpGet]
        public IActionResult ApplyLeave()
        {
            var model = new LeaveApplicationViewModel();

            LoadEmployee(model);

            return View(model);
        }

        //==================================================
        // Calculate Leave - POST
        //==================================================
        [HttpPost]
        public IActionResult CalculateLeave(LeaveApplicationViewModel model)
        {
            LoadEmployee(model);

            // Validation
            if (model.ToDate < model.FromDate)
            {
                ModelState.AddModelError("", "To Date cannot be earlier than From Date.");
                ModelState.Remove(nameof(model.TotalDays));
                ModelState.Remove(nameof(model.CLDays));
                ModelState.Remove(nameof(model.LOPDays));

                return View("ApplyLeave", model);
            }

            // Calculate Total Days
            model.TotalDays = (decimal)(model.ToDate - model.FromDate).TotalDays + 1;

            // Half Day
            if (model.LeaveSession == "Morning" ||
                model.LeaveSession == "Afternoon")
            {
                if (model.FromDate != model.ToDate)
                {
                    ModelState.AddModelError("", "Half Day leave can only be applied for a single date.");

                    ModelState.Remove(nameof(model.TotalDays));
                    ModelState.Remove(nameof(model.CLDays));
                    ModelState.Remove(nameof(model.LOPDays));

                    return View("ApplyLeave", model);
                }

                model.TotalDays = 0.5M;
            }

            // CL / LOP Calculation
            if (model.TotalDays <= model.LeaveBalance)
            {
                model.CLDays = model.TotalDays;
                model.LOPDays = 0;
            }
            else
            {
                model.CLDays = model.LeaveBalance;
                model.LOPDays = model.TotalDays - model.LeaveBalance;
            }

            ModelState.Remove(nameof(model.TotalDays));
            ModelState.Remove(nameof(model.CLDays));
            ModelState.Remove(nameof(model.LOPDays));

            return View("ApplyLeave", model);
        }
        //==================================================
        // Apply Leave - POST
        //==================================================
        [HttpPost]
        public IActionResult ApplyLeaveSave(LeaveApplicationViewModel model)
        {
            LoadEmployee(model);

            // Reason is mandatory only while applying
            if (string.IsNullOrWhiteSpace(model.Reason))
            {
                ModelState.AddModelError(nameof(model.Reason), "Reason is required.");

                return View("ApplyLeave", model);
            }

            // Date Validation
            if (model.ToDate < model.FromDate)
            {
                ModelState.AddModelError("", "To Date cannot be earlier than From Date.");

                return View("ApplyLeave", model);
            }

            // Calculate Total Days
            model.TotalDays = (decimal)(model.ToDate - model.FromDate).TotalDays + 1;

            // Half Day Validation
            if (model.LeaveSession == "Morning" ||
                model.LeaveSession == "Afternoon")
            {
                if (model.FromDate != model.ToDate)
                {
                    ModelState.AddModelError("", "Half Day leave can only be applied for a single date.");

                    return View("ApplyLeave", model);
                }

                model.TotalDays = 0.5M;
            }

            // CL / LOP Calculation
            if (model.TotalDays <= model.LeaveBalance)
            {
                model.CLDays = model.TotalDays;
                model.LOPDays = 0;
            }
            else
            {
                model.CLDays = model.LeaveBalance;
                model.LOPDays = model.TotalDays - model.LeaveBalance;
            }

            //========================================
            // Verify Leave Balance Exists
            //========================================

            bool leaveBalanceExists = _context.EmployeeLeaveBalances
                .Any(x => x.EmployeeId == model.EmployeeId);

            if (!leaveBalanceExists)
            {
                TempData["Error"] =
                    "Your leave balance for the current payroll period has not been uploaded. Please contact HR.";

                return RedirectToAction(nameof(ApplyLeave));
            }

            // Check if employee already has an active leave request
            bool hasPendingLeave = _context.LeaveApplications.Any(x =>
                x.EmployeeId == model.EmployeeId &&
                (
                    x.Status == "Pending" ||
                    x.Status == "Pending Manager Approval" ||
                    x.Status == "Pending - Next Payroll Cycle"
                ));



            if (hasPendingLeave)
            {
                TempData["Error"] =
                    "You already have a leave request awaiting approval. Please wait until it is approved, rejected, or cancelled before applying for another leave.";

                return RedirectToAction(nameof(ApplyLeave));
            }

            // Check for overlapping leave application
            bool overlapExists = _context.LeaveApplications.Any(x =>
                x.EmployeeId == model.EmployeeId &&
                x.Status != "Rejected" &&
                x.Status != "Cancelled" &&
                model.FromDate <= x.ToDate &&
                model.ToDate >= x.FromDate);

            if (overlapExists)
            {
                TempData["Error"] = "A leave application already exists for one or more of the selected dates.";

                return RedirectToAction(nameof(ApplyLeave));
            }

            // Determine Leave Status based on Payroll Cycle
            DateTime currentPayroll = GetPayrollStart(DateTime.Today);

            DateTime leavePayroll = GetPayrollStart(model.FromDate);

            string leaveStatus;

            if (leavePayroll == currentPayroll)
            {
                leaveStatus = "Pending";
            }
            else
            {
                leaveStatus = "Pending - Next Payroll Cycle";
            }

            // Create Entity
            LeaveApplication leave = new LeaveApplication
            {
                EmployeeId = model.EmployeeId,

                LeaveType = "Casual Leave",

                FromDate = model.FromDate,
                ToDate = model.ToDate,

                IsMorningHalf = model.LeaveSession == "Morning",
                IsAfternoonHalf = model.LeaveSession == "Afternoon",

                TotalDays = model.TotalDays,
                CLDays = model.CLDays,
                LOPDays = model.LOPDays,

                Reason = model.Reason,

                EmployeeRemarks = "",

                Status = leaveStatus,

                TeamLeaderStatus = "Pending",

                ManagerStatus = "Pending",

                AppliedOn = DateTime.Now
            };

            _context.LeaveApplications.Add(leave);
            _context.SaveChanges();

            TempData["Success"] = "Leave applied successfully.";

            return RedirectToAction("ApplyLeave");
        }

        //==================================================
        // My Leave History - GET
        //==================================================
        [HttpGet]
        public IActionResult MyLeaveHistory()
        {
            string? employeeCode = HttpContext.Session.GetString("EmployeeCode");

            if (string.IsNullOrWhiteSpace(employeeCode))
                return RedirectToAction("Login", "Account");

            var employee = _context.Employees
                .FirstOrDefault(x => x.EmployeeCode == employeeCode);

            if (employee == null)
                return RedirectToAction("Login", "Account");

            var leaveList = _context.LeaveApplications
                .Where(x => x.EmployeeId == employee.EmployeeId)
                .OrderByDescending(x => x.AppliedOn)
                .ToList();

            return View(leaveList);
        }

        //==================================================
        // Cancel Leave
        //==================================================
        [HttpGet]
        public IActionResult CancelLeave(int id)
        {
            string? employeeCode = HttpContext.Session.GetString("EmployeeCode");

            if (string.IsNullOrWhiteSpace(employeeCode))
                return RedirectToAction("Login", "Account");

            var employee = _context.Employees
                .FirstOrDefault(x => x.EmployeeCode == employeeCode);

            if (employee == null)
                return RedirectToAction("Login", "Account");

            var leave = _context.LeaveApplications
                .FirstOrDefault(x =>
                    x.LeaveApplicationId == id &&
                    x.EmployeeId == employee.EmployeeId);

            if (leave == null)
            {
                TempData["Error"] = "Leave application not found.";

                return RedirectToAction(nameof(MyLeaveHistory));
            }

            if (leave.Status != "Pending" &&
    leave.Status != "Pending - Next Payroll Cycle")
            {
                TempData["Error"] = "Only pending leave applications can be cancelled.";

                return RedirectToAction(nameof(MyLeaveHistory));
            }

            leave.Status = "Cancelled";

            _context.SaveChanges();

            TempData["Success"] = "Leave application cancelled successfully.";

            return RedirectToAction(nameof(MyLeaveHistory));
        }

        //==================================================
        // Common Method
        //==================================================
        private void LoadEmployee(LeaveApplicationViewModel model)
        {
            string? employeeCode = HttpContext.Session.GetString("EmployeeCode");

            if (string.IsNullOrWhiteSpace(employeeCode))
                return;

            var employee = _context.Employees
                .FirstOrDefault(e => e.EmployeeCode == employeeCode);

            if (employee == null)
                return;

            model.EmployeeId = employee.EmployeeId;

            model.Employees = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Value = employee.EmployeeId.ToString(),
                    Text = employee.EmployeeCode + " - " + employee.EmployeeName
                }
            };

            var balance = _context.EmployeeLeaveBalances
                .FirstOrDefault(x => x.EmployeeId == employee.EmployeeId);

            model.LeaveBalance = balance?.CurrentLeaveBalance ?? 0;
        }

        //==================================================
        // Get Payroll Start Date
        //==================================================
        private DateTime GetPayrollStart(DateTime leaveDate)
        {
            if (leaveDate.Day >= 22)
            {
                return new DateTime(leaveDate.Year, leaveDate.Month, 22);
            }

            DateTime previousMonth = leaveDate.AddMonths(-1);

            return new DateTime(previousMonth.Year, previousMonth.Month, 22);
        }

    }
}