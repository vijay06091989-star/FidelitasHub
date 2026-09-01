using FidelitasHub.Data;
using FidelitasHub.Helpers;
using FidelitasHub.Models;
using Microsoft.EntityFrameworkCore;

namespace FidelitasHub.Services.Leave
{
    public class PayrollCycleLeaveService
    {
        private readonly ApplicationDbContext _context;

        public PayrollCycleLeaveService(ApplicationDbContext context)
        {
            _context = context;
        }

        //==================================================
        // Get the actual payroll cycle from PayrollCalendar.
        // Do not calculate the cycle from a hard-coded date rule.
        //==================================================
        public async Task<PayrollCalendar?> GetPayrollCycleForDateAsync(DateTime date)
        {
            DateTime targetDate = date.Date;

            return await _context.PayrollCalendars
                .Where(x =>
                    x.PeriodStart.Date <= targetDate &&
                    x.PeriodEnd.Date >= targetDate)
                .OrderByDescending(x => x.PeriodStart)
                .FirstOrDefaultAsync();
        }

        //==================================================
        // Check whether a specific employee has an imported
        // balance for the specified actual payroll cycle.
        //==================================================
        public async Task<bool> HasBalanceForPayrollCycleAsync(
            int employeeId,
            PayrollCalendar payroll)
        {
            DateTime periodStart = payroll.PeriodStart.Date;
            DateTime periodEnd = payroll.PeriodEnd.Date;

            return await _context.EmployeeLeaveBalances
                .AnyAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.BalancePeriodStart.HasValue &&
                    x.BalancePeriodEnd.HasValue &&
                    x.BalancePeriodStart.Value.Date == periodStart &&
                    x.BalancePeriodEnd.Value.Date == periodEnd);
        }

        //==================================================
        // Get a specific employee's balance for a specific
        // actual payroll cycle.
        //==================================================
        public async Task<EmployeeLeaveBalance?> GetBalanceForPayrollCycleAsync(
            int employeeId,
            PayrollCalendar payroll)
        {
            DateTime periodStart = payroll.PeriodStart.Date;
            DateTime periodEnd = payroll.PeriodEnd.Date;

            return await _context.EmployeeLeaveBalances
                .FirstOrDefaultAsync(x =>
                    x.EmployeeId == employeeId &&
                    x.BalancePeriodStart.HasValue &&
                    x.BalancePeriodEnd.HasValue &&
                    x.BalancePeriodStart.Value.Date == periodStart &&
                    x.BalancePeriodEnd.Value.Date == periodEnd);
        }

        //==================================================
        // Compatibility helper for existing callers.
        //==================================================
        public async Task<bool> HasCurrentCycleBalanceAsync(int employeeId)
        {
            var currentPayroll = await GetPayrollCycleForDateAsync(
                DateTimeHelper.GetIST().Date);

            if (currentPayroll == null)
            {
                return false;
            }

            return await HasBalanceForPayrollCycleAsync(
                employeeId,
                currentPayroll);
        }


        //==================================================
        // Synchronize current payroll cycle
        // Compatibility method for existing callers
        //==================================================

        public async Task SynchronizeAsync()
        {
            var payroll = await GetPayrollCycleForDateAsync(
                DateTimeHelper.GetIST().Date);

            if (payroll == null)
            {
                return;
            }

            await SynchronizeAsync(payroll);
        }


        //==================================================
        // Synchronize held leave applications for the current
        // payroll cycle using the actual PayrollCalendar dates.
        //==================================================
        public async Task SynchronizeAsync(
    PayrollCalendar payroll)
        {
            var leaves = await _context.LeaveApplications
                .Include(x => x.Employee)
                .Where(x =>
                    x.Status == "Pending" ||
                    x.Status == "Pending Manager Approval" ||
                    x.Status == "Pending - Next Payroll Cycle" ||
                    x.Status == "Pending - Balance Import")
                .ToListAsync();

            foreach (var leave in leaves)
            {
                var leavePayroll =
                    await GetPayrollCycleForDateAsync(
                        leave.FromDate.Date);

                if (leavePayroll == null ||
                    leavePayroll.PayrollCalendarId !=
                    payroll.PayrollCalendarId)
                {
                    continue;
                }

                var balance =
                    await GetBalanceForPayrollCycleAsync(
                        leave.EmployeeId,
                        payroll);

                if (balance == null)
                {
                    HoldForBalanceImport(leave);
                    continue;
                }

                if (leave.Status ==
                        "Pending - Balance Import" ||
                    leave.Status ==
                        "Pending - Next Payroll Cycle")
                {
                    RecalculateLeaveDays(
                        leave,
                        balance.CurrentLeaveBalance);

                    RouteForApproval(leave);
                }
            }

            await _context.SaveChangesAsync();
        }

        //==================================================
        // Compatibility helper retained so older code can
        // compile. Completion is now determined from actual
        // imported balance rows, not SystemSettings.
        //==================================================
        public Task MarkCurrentCycleBalanceImportCompletedAsync(DateTime payrollStart)
        {
            return Task.CompletedTask;
        }

        //==================================================
        // Route leave to Team Leader or Manager.
        //==================================================
        private static void RouteForApproval(LeaveApplication leave)
        {
            bool hasTeamLeader =
                leave.Employee != null &&
                leave.Employee.ReportingTeamLeaderId.HasValue;

            if (hasTeamLeader)
            {
                leave.Status = "Pending";
                leave.TeamLeaderStatus = "Pending";
                leave.ManagerStatus = "Pending";
            }
            else
            {
                leave.Status = "Pending Manager Approval";
                leave.TeamLeaderStatus = "Not Required";
                leave.ManagerStatus = "Pending";
            }
        }

        //==================================================
        // Hold leave until this employee's current payroll
        // cycle balance is available.
        //==================================================
        private static void HoldForBalanceImport(LeaveApplication leave)
        {
            leave.Status = "Pending - Balance Import";

            leave.TeamLeaderStatus =
                leave.TeamLeaderStatus == "Not Required"
                    ? "Not Required"
                    : "Pending";

            leave.ManagerStatus = "Pending";
            leave.CLDays = 0;
            leave.LOPDays = 0;
        }

        //==================================================
        // Calculate CL / LOP from the imported balance.
        //==================================================
        private static void RecalculateLeaveDays(
            LeaveApplication leave,
            decimal leaveBalance)
        {
            if (leave.TotalDays <= leaveBalance)
            {
                leave.CLDays = leave.TotalDays;
                leave.LOPDays = 0;
            }
            else
            {
                leave.CLDays = leaveBalance;
                leave.LOPDays = leave.TotalDays - leaveBalance;
            }
        }
    }
}
