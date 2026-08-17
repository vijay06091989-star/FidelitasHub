using FidelitasHub.Data;
using FidelitasHub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FidelitasHub.Controllers
{
    public class LeaveApprovalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ReportingService _reportingService;

        public LeaveApprovalController(
            ApplicationDbContext context,
            ReportingService reportingService)
        {
            _context = context;
            _reportingService = reportingService;
        }

        //==================================================
        // Leave Approval List
        //==================================================

        public async Task<IActionResult> Index()
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
            // ADMIN / SUPER ADMIN
            //
            // Admin can see all pending Team Leader approvals
            //==================================================

            if (currentEmployee.Role == "Admin" ||
                currentEmployee.Role == "SuperAdmin")
            {
                var adminLeaves = await _context.LeaveApplications
                    .Include(x => x.Employee)
                    .Where(x =>
                        x.Status == "Pending")
                    .OrderByDescending(x => x.AppliedOn)
                    .ToListAsync();

                return View(adminLeaves);
            }

            //==================================================
            // TEAM LEADER
            //
            // Team Leader can see only employees whose
            // ReportingTeamLeaderId points to this Team Leader
            //==================================================

            if (currentEmployee.Role == "Team Leader")
            {
                var teamLeaderLeaves =
                    await _context.LeaveApplications
                        .Include(x => x.Employee)
                        .Where(x =>
                            x.Status == "Pending" &&
                            x.Employee.ReportingTeamLeaderId ==
                                currentEmployee.EmployeeId)
                        .OrderByDescending(x => x.AppliedOn)
                        .ToListAsync();

                return View(teamLeaderLeaves);
            }

            //==================================================
            // OTHER ROLES
            //==================================================

            return Forbid();
        }


        //==================================================
        // Team Leader Approve
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
            else if (currentEmployee.Role == "Team Leader")
            {
                // Team Leader can approve only their
                // assigned employees.

                if (leave.Employee.ReportingTeamLeaderId !=
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
            // Status Check
            //
            // Team Leader can approve only a leave which
            // is currently waiting for Team Leader approval.
            //==================================================

            if (leave.Status != "Pending")
            {
                TempData["Error"] =
                    "This leave is no longer pending Team Leader approval.";

                return RedirectToAction(nameof(Index));
            }

            //==================================================
            // Team Leader Approval
            //==================================================

            leave.TeamLeaderStatus =
                "Approved";

            leave.TeamLeaderApprovalDate =
                DateTime.Now;

            //==================================================
            // Forward to Manager
            //==================================================

            leave.Status =
                "Pending Manager Approval";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Leave approved successfully and forwarded to Manager.";

            return RedirectToAction(nameof(Index));
        }


        //==================================================
        // Team Leader Reject
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
            else if (currentEmployee.Role == "Team Leader")
            {
                // Team Leader can reject only their
                // assigned employees.

                if (leave.Employee.ReportingTeamLeaderId !=
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
            // Status Check
            //==================================================

            if (leave.Status != "Pending")
            {
                TempData["Error"] =
                    "This leave is no longer pending Team Leader approval.";

                return RedirectToAction(nameof(Index));
            }

            //==================================================
            // Team Leader Rejection
            //==================================================

            leave.TeamLeaderStatus =
                "Rejected";

            leave.TeamLeaderApprovalDate =
                DateTime.Now;

            leave.Status =
                "Rejected";

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Leave rejected successfully.";

            return RedirectToAction(nameof(Index));
        }
    }
}