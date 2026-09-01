using FidelitasHub.Data;
using FidelitasHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using FidelitasHub.Helpers;
using FidelitasHub.Services.Leave;

namespace FidelitasHub.Controllers
{
    public class LeaveController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PayrollCycleLeaveService _payrollCycleLeaveService;

        public LeaveController(
            ApplicationDbContext context,
            PayrollCycleLeaveService payrollCycleLeaveService)
        {
            _context = context;
            _payrollCycleLeaveService = payrollCycleLeaveService;
        }

        //==================================================
        // Apply Leave - GET
        //==================================================
        [HttpGet]
        public async Task<IActionResult> ApplyLeave()
        {
            await _payrollCycleLeaveService.SynchronizeAsync();

            var model = new LeaveApplicationViewModel();

            DateTime istToday = DateTimeHelper.GetIST().Date;

            model.FromDate = istToday;
            model.ToDate = istToday;

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
        public async Task<IActionResult> ApplyLeaveSave(LeaveApplicationViewModel model)
        {
            await _payrollCycleLeaveService.SynchronizeAsync();

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

            // Check if employee already has an active leave request
            bool hasPendingLeave = _context.LeaveApplications.Any(x =>
                x.EmployeeId == model.EmployeeId &&
                (
                    x.Status == "Pending" ||
                    x.Status == "Pending Manager Approval" ||
                    x.Status == "Pending - Next Payroll Cycle" ||
                    x.Status == "Pending - Balance Import"
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

            //==================================================
            // Determine Employee and Approval Route
            //==================================================

            var employee = _context.Employees
                .FirstOrDefault(e => e.EmployeeId == model.EmployeeId);

            if (employee == null)
            {
                TempData["Error"] =
                    "Employee record could not be found.";

                return RedirectToAction(nameof(ApplyLeave));
            }

            bool hasTeamLeader =
                employee.ReportingTeamLeaderId.HasValue;

            //==================================================
            // Determine Leave Status from the ACTUAL Payroll Calendar
            //==================================================

            var currentPayroll =
                await _payrollCycleLeaveService.GetPayrollCycleForDateAsync(
                    DateTimeHelper.GetIST().Date);

            var leaveStartPayroll =
                await _payrollCycleLeaveService.GetPayrollCycleForDateAsync(
                    model.FromDate.Date);

            var leaveEndPayroll =
                await _payrollCycleLeaveService.GetPayrollCycleForDateAsync(
                    model.ToDate.Date);

            if (currentPayroll == null)
            {
                TempData["Error"] =
                    "No active payroll cycle exists for today. Please create the payroll calendar first.";

                return RedirectToAction(nameof(ApplyLeave));
            }

            if (leaveStartPayroll == null || leaveEndPayroll == null)
            {
                TempData["Error"] =
                    "The selected leave date is outside the configured payroll calendar.";

                return RedirectToAction(nameof(ApplyLeave));
            }

            if (leaveStartPayroll.PayrollCalendarId !=
                leaveEndPayroll.PayrollCalendarId)
            {
                TempData["Error"] =
                    "A single leave application cannot span two payroll cycles. Please apply separately for each cycle.";

                return RedirectToAction(nameof(ApplyLeave));
            }

            string leaveStatus;

            if (leaveStartPayroll.PayrollCalendarId ==
                currentPayroll.PayrollCalendarId)
            {
                bool currentCycleBalanceImported =
                    await _payrollCycleLeaveService.HasBalanceForPayrollCycleAsync(
                        model.EmployeeId,
                        currentPayroll);

                if (currentCycleBalanceImported)
                {
                    leaveStatus = hasTeamLeader
                        ? "Pending"
                        : "Pending Manager Approval";
                }
                else
                {
                    leaveStatus = "Pending - Balance Import";
                    model.CLDays = 0;
                    model.LOPDays = 0;
                }
            }
            else
            {
                leaveStatus = "Pending - Next Payroll Cycle";
                model.CLDays = 0;
                model.LOPDays = 0;
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

                TeamLeaderStatus =
    hasTeamLeader
        ? "Pending"
        : "Not Required",

                ManagerStatus = "Pending",

                AppliedOn = DateTimeHelper.GetIST()
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
                leave.Status != "Pending - Next Payroll Cycle" &&
                leave.Status != "Pending - Balance Import")
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

            DateTime today = DateTimeHelper.GetIST().Date;

            var currentPayroll = _context.PayrollCalendars
                .Where(x =>
                    x.PeriodStart.Date <= today &&
                    x.PeriodEnd.Date >= today)
                .OrderByDescending(x => x.PeriodStart)
                .FirstOrDefault();

            if (currentPayroll == null)
            {
                model.LeaveBalance = 0;
                return;
            }

            DateTime periodStart = currentPayroll.PeriodStart.Date;
            DateTime periodEnd = currentPayroll.PeriodEnd.Date;

            var balance = _context.EmployeeLeaveBalances
                .FirstOrDefault(x =>
                    x.EmployeeId == employee.EmployeeId &&
                    x.BalancePeriodStart.HasValue &&
                    x.BalancePeriodEnd.HasValue &&
                    x.BalancePeriodStart.Value.Date == periodStart &&
                    x.BalancePeriodEnd.Value.Date == periodEnd);

            model.LeaveBalance = balance?.CurrentLeaveBalance ?? 0;
        }

        //==================================================
        // Leave Register (all applications)
        //==================================================

        public IActionResult LeaveRegister(
            string status,
            int? employeeId,
            DateTime? fromDate,
            DateTime? toDate)
        {
            var leaves = _context.LeaveApplications
                .AsQueryable();


            // Status Filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                leaves = leaves.Where(l => l.Status == status);
            }


            // Employee Filter
            if (employeeId.HasValue)
            {
                leaves = leaves.Where(l => l.EmployeeId == employeeId.Value);
            }


            // From Date Filter
            if (fromDate.HasValue)
            {
                leaves = leaves.Where(l => l.FromDate >= fromDate.Value);
            }


            // To Date Filter
            if (toDate.HasValue)
            {
                leaves = leaves.Where(l => l.ToDate <= toDate.Value);
            }


            // Employee dropdown
            ViewBag.Employees = _context.Employees
                .Select(e => new SelectListItem
                {
                    Value = e.EmployeeId.ToString(),
                    Text = e.EmployeeCode + " - " + e.EmployeeName
                })
                .OrderBy(e => e.Text)
                .ToList();


            ViewBag.EmployeeNames = _context.Employees
                .ToDictionary(e => e.EmployeeId, e => e.EmployeeName);


            // Keep existing cards
            ViewBag.Status = status;

            ViewBag.Total = _context.LeaveApplications.Count();

            ViewBag.Pending =
                _context.LeaveApplications.Count(l => l.Status == "Pending");

            ViewBag.Approved =
                _context.LeaveApplications.Count(l => l.Status == "Approved");

            ViewBag.Cancelled =
                _context.LeaveApplications.Count(l => l.Status == "Cancelled");


            // Preserve search values
            ViewBag.SelectedEmployee = employeeId;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");


            return View(
                leaves
                .OrderByDescending(l => l.AppliedOn)
                .ToList()
            );
        }

        //==================================================
        // Permission Register
        //==================================================

        [HttpGet]
        public IActionResult PermissionRegister()
        {
            return View();
        }
    }
}