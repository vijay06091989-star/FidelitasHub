using FidelitasHub.Data;
using FidelitasHub.Models;
using FidelitasHub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FidelitasHub.Controllers
{
    public class ManagerApprovalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ReportingService _reportingService;

        public ManagerApprovalController(
            ApplicationDbContext context,
            ReportingService reportingService)
        {
            _context = context;
            _reportingService = reportingService;
        }

        //==================================================
        // Manager Approval List
        //==================================================

        public async Task<IActionResult> Index()
        {
            var currentEmployee = _reportingService.GetCurrentEmployee();

            //==================================================
            // Not Logged In
            //==================================================

            if (currentEmployee == null)
            {
                return RedirectToAction("Login", "Account");
            }

            //==================================================
            // ADMIN / SUPER ADMIN
            //
            // Can see all pending Manager approvals
            //==================================================

            if (currentEmployee.Role == "Admin" ||
                currentEmployee.Role == "SuperAdmin")
            {
                var adminLeaves = await _context.LeaveApplications
                    .Include(x => x.Employee)
                    .Where(x =>
                        x.Status == "Pending Manager Approval")
                    .OrderByDescending(x => x.AppliedOn)
                    .ToListAsync();

                return View(adminLeaves);
            }

            //==================================================
            // MANAGER
            //
            // Can see only employees whose
            // ReportingManagerId points to this Manager
            //==================================================

            if (currentEmployee.Role == "Manager")
            {
                var managerLeaves = await _context.LeaveApplications
                    .Include(x => x.Employee)
                    .Where(x =>
                        x.Status == "Pending Manager Approval" &&
                        x.Employee.ReportingManagerId ==
                            currentEmployee.EmployeeId)
                    .OrderByDescending(x => x.AppliedOn)
                    .ToListAsync();

                return View(managerLeaves);
            }

            //==================================================
            // OTHER ROLES
            //==================================================

            return Forbid();
        }


        //==================================================
        // Manager Approve
        //==================================================

        public async Task<IActionResult> Approve(int id)
        {
            var currentEmployee =
                _reportingService.GetCurrentEmployee();

            //==================================================
            // Not Logged In
            //==================================================

            if (currentEmployee == null)
            {
                return RedirectToAction("Login", "Account");
            }

            //==================================================
            // Get Leave Application + Employee
            //==================================================

            var leave = await _context.LeaveApplications
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x =>
                    x.LeaveApplicationId == id);

            if (leave == null)
            {
                return NotFound();
            }

            //==================================================
            // SECURITY CHECK
            //==================================================

            if (currentEmployee.Role == "Admin" ||
                currentEmployee.Role == "SuperAdmin")
            {
                // Admin / SuperAdmin allowed
            }
            else if (currentEmployee.Role == "Manager")
            {
                // Manager can approve only their own employees

                if (leave.Employee.ReportingManagerId !=
                    currentEmployee.EmployeeId)
                {
                    return Forbid();
                }
            }
            else
            {
                return Forbid();
            }

            //==================================================
            // Already Approved
            //==================================================

            if (leave.Status == "Approved")
            {
                TempData["Error"] =
                    "This leave has already been approved.";

                return RedirectToAction(nameof(Index));
            }

            //==================================================
            // Calculate CL / LOP
            //==================================================

            // IMPORTANT:
            // An employee can have multiple leave-balance rows.
            // Use the imported balance for the payroll period that
            // actually contains this leave date. Historical rows
            // (including rows with NULL payroll periods) must not be
            // used for a current-period approval.
            DateTime leaveDate = leave.FromDate.Date;

            var leaveBalance =
                await _context.EmployeeLeaveBalances
                    .Where(x =>
                        x.EmployeeId == leave.EmployeeId &&
                        x.BalancePeriodStart.HasValue &&
                        x.BalancePeriodEnd.HasValue &&
                        x.BalancePeriodStart.Value.Date <= leaveDate &&
                        x.BalancePeriodEnd.Value.Date >= leaveDate)
                    .OrderByDescending(x => x.BalancePeriodStart)
                    .FirstOrDefaultAsync();

            decimal availableBalance =
                leaveBalance?.CurrentLeaveBalance ?? 0;

            if (availableBalance >= leave.TotalDays)
            {
                leave.CLDays = leave.TotalDays;
                leave.LOPDays = 0;

                if (leaveBalance != null)
                {
                    leaveBalance.CurrentLeaveBalance -=
                        leave.TotalDays;

                    leaveBalance.LastUpdatedOn =
                        DateTime.Now;

                    leaveBalance.LastUpdatedBy =
                        "Manager";
                }
            }
            else
            {
                leave.CLDays = availableBalance;

                leave.LOPDays =
                    leave.TotalDays - availableBalance;

                if (leaveBalance != null)
                {
                    leaveBalance.CurrentLeaveBalance = 0;

                    leaveBalance.LastUpdatedOn =
                        DateTime.Now;

                    leaveBalance.LastUpdatedBy =
                        "Manager";
                }
            }

            //==================================================
            // Final Approval
            //==================================================

            leave.ManagerStatus = "Approved";

            leave.ManagerApprovalDate =
                DateTime.Now;

            leave.Status = "Approved";

            //==================================================
            // Create / Update Attendance Records
            //==================================================

            decimal remainingCLDays =
                leave.CLDays;

            DateTime currentDate =
                leave.FromDate;

            while (currentDate <= leave.ToDate)
            {
                Attendance? attendance =
                    await _context.Attendances
                        .FirstOrDefaultAsync(a =>
                            a.EmployeeId ==
                                leave.EmployeeId &&

                            a.AttendanceDate.Date ==
                                currentDate.Date);

                if (attendance == null)
                {
                    attendance = new Attendance
                    {
                        EmployeeId =
                            leave.EmployeeId,

                        AttendanceDate =
                            currentDate
                    };

                    _context.Attendances.Add(
                        attendance);
                }

                bool isFullDayLeave =
                    !leave.IsMorningHalf &&
                    !leave.IsAfternoonHalf;

                bool isHalfDayLeave =
                    leave.IsMorningHalf ||
                    leave.IsAfternoonHalf;

                //==================================================
                // ATTENDANCE CLASSIFICATION
                //==================================================
                //
                // Full Day:
                //     AttendanceStatus = Approved Leave / LOP
                //     Status = On Leave
                //
                // Half Day:
                //     AttendanceStatus = Half Day
                //     Status = Not Punched In
                //
                // Half-day leave must NOT lock attendance.
                // The employee must still be able to Punch In.
                //

                if (attendance.PunchIn == null)
                {
                    if (isFullDayLeave)
                    {
                        attendance.AttendanceStatus =
                            remainingCLDays > 0
                                ? "Approved Leave"
                                : "LOP";

                        attendance.Status =
                            "On Leave";

                        attendance.PunchOut =
                            null;

                        attendance.PunchOutMode =
                            "Leave";
                    }
                    else
                    {
                        // Half-day leave.
                        // Employee is still expected
                        // to work part of the day.

                        attendance.AttendanceStatus =
                            "Half Day";

                        attendance.Status =
                            "Not Punched In";

                        attendance.PunchOut =
                            null;

                        attendance.PunchOutMode =
                            "Manual";
                    }
                }
                else if (isHalfDayLeave)
                {
                    // Employee was already working when
                    // the half-day leave was approved.

                    attendance.AttendanceStatus =
                        "Half Day";

                    // DO NOT change:
                    // Working / On Break / Punched Out
                }

                //==================================================
                // Deduct CL Day
                //==================================================

                if (remainingCLDays > 0)
                {
                    remainingCLDays -= 1;
                }

                currentDate =
                    currentDate.AddDays(1);
            }

            //==================================================
            // Save Changes
            //==================================================

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Leave approved successfully.";

            return RedirectToAction(nameof(Index));
        }


        //==================================================
        // Manager Reject
        //==================================================

        public async Task<IActionResult> Reject(int id)
        {
            var currentEmployee =
                _reportingService.GetCurrentEmployee();

            //==================================================
            // Not Logged In
            //==================================================

            if (currentEmployee == null)
            {
                return RedirectToAction("Login", "Account");
            }

            //==================================================
            // Get Leave Application + Employee
            //==================================================

            var leave = await _context.LeaveApplications
                .Include(x => x.Employee)
                .FirstOrDefaultAsync(x =>
                    x.LeaveApplicationId == id);

            if (leave == null)
            {
                return NotFound();
            }

            //==================================================
            // SECURITY CHECK
            //==================================================

            if (currentEmployee.Role == "Admin" ||
                currentEmployee.Role == "SuperAdmin")
            {
                // Admin / SuperAdmin allowed
            }
            else if (currentEmployee.Role == "Manager")
            {
                // Manager can reject only their own employees

                if (leave.Employee.ReportingManagerId !=
                    currentEmployee.EmployeeId)
                {
                    return Forbid();
                }
            }
            else
            {
                return Forbid();
            }

            //==================================================
            // Reject
            //==================================================

            leave.ManagerStatus =
                "Rejected";

            leave.ManagerApprovalDate =
                DateTime.Now;

            leave.Status =
                "Rejected";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Leave rejected.";

            return RedirectToAction(nameof(Index));
        }
    }
}