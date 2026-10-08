using FidelitasHub.Data;
using FidelitasHub.Helpers;
using FidelitasHub.Models;
using Microsoft.EntityFrameworkCore;

namespace FidelitasHub.Services
{
    public class ProductivityService
    {
        private readonly ApplicationDbContext _context;

        public ProductivityService(ApplicationDbContext context)
        {
            _context = context;
        }

        //==================================================
        // PAYROLL CYCLE
        //==================================================
        public PayrollCalendar? GetPayrollCycleForDate(DateTime date)
        {
            var targetDate = date.Date;
            return _context.PayrollCalendars
                .Where(x => x.PeriodStart.Date <= targetDate && x.PeriodEnd.Date >= targetDate)
                .OrderByDescending(x => x.PeriodStart)
                .FirstOrDefault();
        }

        public PayrollCalendar? GetCurrentPayrollCycle()
            => GetPayrollCycleForDate(DateTimeHelper.GetIST().Date);

        //==================================================
        // DIRECT ASSIGNMENTS
        //==================================================
        public IQueryable<ProductivityAssignment> ActiveAssignments(DateTime date)
        {
            var day = date.Date;
            return _context.ProductivityAssignments
                .Include(x => x.Employee)
                .Include(x => x.Client)
                .Include(x => x.Activity!)
                    .ThenInclude(a => a.Process)
                .Where(x => x.IsActive &&
                            x.EffectiveFrom.Date <= day &&
                            (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= day));
        }

        //==================================================
        // EFFECTIVE ASSIGNMENTS
        // Direct employee assignments win over group assignments.
        // Specific-client group assignments win over ALL CLIENTS.
        //==================================================
        public List<ProductivityAssignment> GetEffectiveAssignments(int employeeId, DateTime date)
        {
            var day = date.Date;

            var direct = ActiveAssignments(day)
                .Where(x => x.EmployeeId == employeeId)
                .ToList();

            var directKeys = direct
                .Select(x => (x.ClientId, x.ProductivityActivityId))
                .ToHashSet();

            var memberships = _context.ProductivityGroupMembers
                .Include(x => x.Group)
                .Where(x => x.EmployeeId == employeeId && x.IsActive &&
                            x.EffectiveFrom.Date <= day &&
                            (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= day))
                .ToList();

            if (memberships.Count == 0)
                return direct;

            var groupIds = memberships.Select(x => x.ProductivityGroupId).Distinct().ToList();
            var groupAssignments = _context.ProductivityGroupAssignments
                .Include(x => x.Group)
                .Include(x => x.Client)
                .Include(x => x.Activity!)
                    .ThenInclude(a => a.Process)
                .Where(x => groupIds.Contains(x.ProductivityGroupId) && x.IsActive &&
                            x.EffectiveFrom.Date <= day &&
                            (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= day))
                .ToList();

            var clients = _context.Clients
                .Where(x => x.IsActive)
                .OrderBy(x => x.ClientCode)
                .ToList();

            var effective = new List<ProductivityAssignment>(direct);
            var groupCandidates = new Dictionary<(int ClientId, int ActivityId), (ProductivityGroupAssignment Assignment, bool Specific)>();

            // Resolve specific-client group assignments first. This guarantees
            // that a specific client override wins over an ALL CLIENTS rule
            // regardless of database row order.
            foreach (var ga in groupAssignments.Where(x => x.ClientId.HasValue))
            {
                var key = (ga.ClientId!.Value, ga.ProductivityActivityId);
                if (!groupCandidates.ContainsKey(key))
                    groupCandidates[key] = (ga, true);
            }

            foreach (var ga in groupAssignments.Where(x => !x.ClientId.HasValue))
            {
                foreach (var client in clients)
                {
                    var key = (client.ClientId, ga.ProductivityActivityId);
                    if (!groupCandidates.ContainsKey(key))
                        groupCandidates[key] = (ga, false);
                }
            }

            foreach (var candidate in groupCandidates)
            {
                if (directKeys.Contains(candidate.Key))
                    continue;

                var ga = candidate.Value.Assignment;
                effective.Add(new ProductivityAssignment
                {
                    ProductivityAssignmentId = 0,
                    EmployeeId = employeeId,
                    Employee = direct.FirstOrDefault()?.Employee ?? _context.Employees.FirstOrDefault(e => e.EmployeeId == employeeId),
                    ClientId = candidate.Key.ClientId,
                    Client = ga.ClientId.HasValue
                        ? ga.Client
                        : clients.FirstOrDefault(c => c.ClientId == candidate.Key.ClientId),
                    ProductivityActivityId = ga.ProductivityActivityId,
                    Activity = ga.Activity,
                    TargetPerDay = ga.TargetPerDay,
                    Weight = ga.Weight,
                    EffectiveFrom = ga.EffectiveFrom,
                    EffectiveTo = ga.EffectiveTo,
                    IsActive = true,
                    CreatedOn = ga.CreatedOn,
                    CreatedBy = ga.CreatedBy
                });
            }

            return effective
                .OrderBy(x => x.Client?.ClientCode)
                .ThenBy(x => x.Activity?.Process?.ProcessName)
                .ThenBy(x => x.Activity?.ActivityName)
                .ToList();
        }

        public ProductivityAssignment? FindAssignment(int employeeId, int clientId, int activityId, DateTime date)
        {
            return GetEffectiveAssignments(employeeId, date)
                .FirstOrDefault(x => x.ClientId == clientId && x.ProductivityActivityId == activityId);
        }

        //==================================================
        // WORKING DAYS
        //==================================================
        public int WorkingDays(DateTime from, DateTime to, int employeeId)
        {
            from = from.Date;
            to = to.Date;
            if (to < from) return 0;

            var approvedLeaveDates = new HashSet<DateTime>();
            var leaves = _context.LeaveApplications
                .Where(x => x.EmployeeId == employeeId &&
                            x.Status == "Approved" &&
                            x.ToDate.Date >= from &&
                            x.FromDate.Date <= to)
                .ToList();

            foreach (var leave in leaves)
            {
                var start = leave.FromDate.Date < from ? from : leave.FromDate.Date;
                var end = leave.ToDate.Date > to ? to : leave.ToDate.Date;
                for (var d = start; d <= end; d = d.AddDays(1))
                {
                    if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                        approvedLeaveDates.Add(d);
                }
            }

            var count = 0;
            for (var d = from; d <= to; d = d.AddDays(1))
            {
                if (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday)
                    continue;
                if (approvedLeaveDates.Contains(d))
                    continue;
                count++;
            }
            return count;
        }

        //==================================================
        // REPORTING
        //==================================================
        public List<ProductivityReportRow> BuildReport(DateTime from, DateTime to, int? employeeId, int? clientId, int? processId)
        {
            var entries = _context.ProductivityEntries
                .Include(x => x.Employee)
                .Include(x => x.Client)
                .Include(x => x.Activity!)
                    .ThenInclude(a => a.Process)
                .Where(x => x.ProductionDate.Date >= from.Date && x.ProductionDate.Date <= to.Date);

            if (employeeId.HasValue) entries = entries.Where(x => x.EmployeeId == employeeId.Value);
            if (clientId.HasValue) entries = entries.Where(x => x.ClientId == clientId.Value);
            if (processId.HasValue) entries = entries.Where(x => x.Activity!.ProductivityProcessId == processId.Value);

            var entryRows = entries.ToList()
                .GroupBy(x => new
                {
                    x.EmployeeId,
                    EmployeeCode = x.Employee!.EmployeeCode,
                    EmployeeName = x.Employee.EmployeeName,
                    x.ClientId,
                    ClientCode = x.Client!.ClientCode,
                    ClientName = x.Client.ClientName,
                    ProcessId = x.Activity!.ProductivityProcessId,
                    ProcessName = x.Activity.Process!.ProcessName,
                    ActivityId = x.ProductivityActivityId,
                    ActivityName = x.Activity.ActivityName
                })
                .Select(g => new ProductivityReportRow
                {
                    EmployeeId = g.Key.EmployeeId,
                    EmployeeCode = g.Key.EmployeeCode,
                    EmployeeName = g.Key.EmployeeName,
                    ClientId = g.Key.ClientId,
                    ActivityId = g.Key.ActivityId,
                    ClientCode = g.Key.ClientCode,
                    ClientName = g.Key.ClientName,
                    ProcessName = g.Key.ProcessName,
                    ActivityName = g.Key.ActivityName,
                    Actual = g.Sum(x => x.Quantity),
                    WeightedAchievement = g.Sum(x => x.WeightedAchievement),
                    Target = 0
                })
                .ToList();

            foreach (var row in entryRows)
            {
                var assignment = GetEffectiveAssignments(row.EmployeeId, from)
                    .Where(x => x.ClientId == row.ClientId &&
                                x.ProductivityActivityId == row.ActivityId &&
                                x.EffectiveFrom.Date <= to.Date &&
                                (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= from.Date))
                    .OrderByDescending(x => x.ProductivityAssignmentId > 0)
                    .ThenByDescending(x => x.EffectiveFrom)
                    .FirstOrDefault();

                var activity = _context.ProductivityActivities
                    .Include(x => x.Process)
                    .FirstOrDefault(x => x.ProductivityActivityId == row.ActivityId);

                if (assignment != null)
                {
                    var effectiveFrom = assignment.EffectiveFrom.Date > from.Date ? assignment.EffectiveFrom.Date : from.Date;
                    var effectiveTo = assignment.EffectiveTo.HasValue && assignment.EffectiveTo.Value.Date < to.Date
                        ? assignment.EffectiveTo.Value.Date : to.Date;
                    row.Target = assignment.TargetPerDay * WorkingDays(effectiveFrom, effectiveTo, row.EmployeeId);
                }
                else if (activity != null)
                {
                    row.Target = activity.DefaultTargetPerDay * WorkingDays(from, to, row.EmployeeId);
                }
            }

            return entryRows.OrderBy(x => x.ClientCode).ThenBy(x => x.EmployeeCode).ThenBy(x => x.ProcessName).ThenBy(x => x.ActivityName).ToList();
        }
    }
}
