using FidelitasHub.Data;
using FidelitasHub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FidelitasHub.Controllers
{
    public class ManagerApprovalController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ManagerApprovalController(ApplicationDbContext context)
        {
            _context = context;
        }

        //==================================================
        // Manager Approval List
        //==================================================
        public async Task<IActionResult> Index()
        {
            var leaves = await _context.LeaveApplications
                .Include(x => x.Employee)
                .Where(x => x.Status == "Pending Manager Approval")
                .OrderByDescending(x => x.AppliedOn)
                .ToListAsync();

            return View(leaves);
        }

        //==================================================
        // Manager Approve
        //==================================================
        public async Task<IActionResult> Approve(int id)
        {
            var leave = await _context.LeaveApplications
                .FirstOrDefaultAsync(x => x.LeaveApplicationId == id);

            if (leave == null)
                return NotFound();

            if (leave.Status == "Approved")
            {
                TempData["Error"] = "This leave has already been approved.";
                return RedirectToAction(nameof(Index));
            }

            //========================================
            // Calculate CL / LOP
            //========================================

            var leaveBalance = await _context.EmployeeLeaveBalances
                .FirstOrDefaultAsync(x => x.EmployeeId == leave.EmployeeId);

            decimal availableBalance = leaveBalance?.CurrentLeaveBalance ?? 0;

            if (availableBalance >= leave.TotalDays)
            {
                leave.CLDays = leave.TotalDays;
                leave.LOPDays = 0;

                if (leaveBalance != null)
                {
                    leaveBalance.CurrentLeaveBalance -= leave.TotalDays;
                    leaveBalance.LastUpdatedOn = DateTime.Now;
                    leaveBalance.LastUpdatedBy = "Manager";
                }
            }
            else
            {
                leave.CLDays = availableBalance;
                leave.LOPDays = leave.TotalDays - availableBalance;

                if (leaveBalance != null)
                {
                    leaveBalance.CurrentLeaveBalance = 0;
                    leaveBalance.LastUpdatedOn = DateTime.Now;
                    leaveBalance.LastUpdatedBy = "Manager";
                }
            }

            //========================================
            // Final Approval
            //========================================

            leave.ManagerStatus = "Approved";
            leave.ManagerApprovalDate = DateTime.Now;
            leave.Status = "Approved";

            //========================================
            // Create / Update Attendance Records
            //========================================

            decimal remainingCLDays = leave.CLDays;

            DateTime currentDate = leave.FromDate;

            while (currentDate <= leave.ToDate)
            {
                Attendance? attendance = await _context.Attendances
                    .FirstOrDefaultAsync(a =>
                        a.EmployeeId == leave.EmployeeId &&
                        a.AttendanceDate.Date == currentDate.Date);

                if (attendance == null)
                {
                    attendance = new Attendance
                    {
                        EmployeeId = leave.EmployeeId,
                        AttendanceDate = currentDate
                    };

                    _context.Attendances.Add(attendance);
                }

                attendance.AttendanceStatus = remainingCLDays > 0
                    ? "Approved Leave"
                    : "LOP";

                attendance.Status = "Punched Out";
                attendance.PunchOutMode = "Manual";

                if (remainingCLDays > 0)
                {
                    remainingCLDays -= 1;
                }

                currentDate = currentDate.AddDays(1);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Leave approved successfully.";

            return RedirectToAction(nameof(Index));
        }

        //==================================================
        // Manager Reject
        //==================================================
        public async Task<IActionResult> Reject(int id)
        {
            var leave = await _context.LeaveApplications
                .FirstOrDefaultAsync(x => x.LeaveApplicationId == id);

            if (leave == null)
                return NotFound();

            leave.ManagerStatus = "Rejected";
            leave.ManagerApprovalDate = DateTime.Now;
            leave.Status = "Rejected";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Leave rejected.";

            return RedirectToAction(nameof(Index));
        }
    }
}