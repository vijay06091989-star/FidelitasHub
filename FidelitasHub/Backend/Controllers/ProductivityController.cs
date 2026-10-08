using FidelitasHub.Data;
using FidelitasHub.Helpers;
using FidelitasHub.Models;
using FidelitasHub.Services;
using FidelitasHub.Services.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace FidelitasHub.Controllers
{
    public class ProductivityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IClientWebLoginProtectionService _webLoginProtection;
        private readonly ReportingService _reportingService;
        private readonly ProductivityService _productivityService;

        public ProductivityController(
            ApplicationDbContext context,
            IClientWebLoginProtectionService webLoginProtection,
            ReportingService reportingService,
            ProductivityService productivityService)
        {
            _context = context;
            _webLoginProtection = webLoginProtection;
            _reportingService = reportingService;
            _productivityService = productivityService;
        }

        public IActionResult Index()
        {
            if (!CanViewProductivity()) return Forbid();

            var current = _reportingService.GetCurrentEmployee();
            if (current == null) return RedirectToAction("Login", "Account");

            var payroll = _productivityService.GetCurrentPayrollCycle();
            var visibleIds = _reportingService.GetVisibleEmployeeIds();

            var rows = payroll == null
                ? new List<ProductivityReportRow>()
                : _productivityService.BuildReport(
                    payroll.PeriodStart.Date,
                    payroll.PeriodEnd.Date,
                    null, null, null)
                    .Where(x => visibleIds.Contains(x.EmployeeId))
                    .ToList();

            ViewBag.ActiveClients = _context.Clients.Count(x => x.IsActive);
            ViewBag.AssignedEmployees = visibleIds.Count;
            ViewBag.ProductivityRecords = _context.ProductivityEntries
                .Count(x => visibleIds.Contains(x.EmployeeId));
            ViewBag.PayrollCycle = payroll;
            ViewBag.PayrollPeriod = payroll == null
                ? "No payroll cycle configured"
                : $"{payroll.PeriodStart:dd-MMM-yyyy} to {payroll.PeriodEnd:dd-MMM-yyyy}";
            ViewBag.PayrollMonth = payroll?.PayrollMonth ?? string.Empty;
            ViewBag.PayrollAchievement = rows.Sum(x => x.Target) <= 0
                ? 0
                : rows.Sum(x => x.Actual) / rows.Sum(x => x.Target) * 100m;

            ViewBag.TopEmployees = rows
                .GroupBy(x => new { x.EmployeeCode, x.EmployeeName })
                .Select(g => new
                {
                    g.Key.EmployeeCode,
                    g.Key.EmployeeName,
                    Target = g.Sum(x => x.Target),
                    Actual = g.Sum(x => x.Actual),
                    Achievement = g.Sum(x => x.Target) <= 0
                        ? 0
                        : g.Sum(x => x.Actual) / g.Sum(x => x.Target) * 100m
                })
                .OrderByDescending(x => x.Achievement)
                .Take(10)
                .ToList();

            return View();
        }

        public IActionResult Client(int id)
        {
            if (!CanManageProductivity()) return Forbid();
            var client = _context.Clients
                .Include(c => c.GeneralShiftManager)
                .Include(c => c.GeneralShiftBillingTeamLeader)
                .Include(c => c.GeneralShiftPostingTeamLeader)
                .Include(c => c.GeneralShiftDMTeamLeader)
                .Include(c => c.GeneralShiftEndToEndTeamLeader)
                .Include(c => c.USShiftManager)
                .Include(c => c.USShiftTeamLeader)
                .FirstOrDefault(c => c.ClientId == id);

            if (client == null) return NotFound();

            ViewBag.CurrentSop = _context.ClientSopDocuments
                .Where(s => s.ClientId == client.ClientId && s.IsCurrent)
                .OrderByDescending(s => s.Version)
                .FirstOrDefault();

            ViewBag.Processes = _context.ProductivityProcesses
                .Include(p => p.Activities)
                .Where(p => p.ClientId == id || p.ClientId == null)
                .OrderBy(p => p.ProcessName)
                .ToList();

            ViewBag.Assignments = _context.ProductivityAssignments
                .Include(a => a.Employee)
                .Include(a => a.Activity!)
                    .ThenInclude(a => a.Process)
                .Where(a => a.ClientId == id && a.IsActive)
                .OrderBy(a => a.Employee!.EmployeeCode)
                .ThenBy(a => a.Activity!.ActivityName)
                .ToList();

            return View(client);
        }

        public IActionResult WebLogins(int id)
        {
            if (!CanManageProductivity()) return Forbid();
            var client = _context.Clients.FirstOrDefault(c => c.ClientId == id);
            if (client == null) return NotFound();

            var webLogins = _context.ClientWebLogins
                .Where(w => w.ClientId == id)
                .OrderBy(w => w.Website)
                .ThenBy(w => w.Username)
                .ToList()
                .Select(w =>
                {
                    var passwordAvailable = _webLoginProtection.TryUnprotect(w.EncryptedPassword, out var password);
                    var securityAvailable = _webLoginProtection.TryUnprotect(w.EncryptedSecurityQuestions, out var securityQuestions);
                    return new ClientWebLoginDisplayViewModel
                    {
                        ClientWebLoginId = w.ClientWebLoginId,
                        ClientId = w.ClientId,
                        Website = w.Website,
                        Url = w.Url,
                        Username = w.Username,
                        Password = passwordAvailable ? password : "[Unavailable]",
                        SecurityQuestions = securityAvailable ? securityQuestions : "[Unavailable]"
                    };
                }).ToList();

            ViewBag.WebLogins = webLogins;
            return View(client);
        }

        //==================================================
        // Productivity Setup
        //==================================================
        [HttpGet]
        public IActionResult Setup(int? processId = null)
        {
            if (!CanManageProductivity()) return Forbid();

            var vm = new ProductivitySetupViewModel
            {
                Processes = _context.ProductivityProcesses
                    .Include(x => x.Client).Include(x => x.Department)
                    .OrderBy(x => x.Client == null ? "" : x.Client.ClientCode)
                    .ThenBy(x => x.ProcessName).ToList(),
                Activities = _context.ProductivityActivities
                    .Include(x => x.Process!).ThenInclude(x => x.Client)
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Process!.Client == null ? "" : x.Process.Client.ClientCode)
                    .ThenBy(x => x.Process!.ProcessName).ThenBy(x => x.ActivityName).ToList(),
                Assignments = _context.ProductivityAssignments
                    .Include(x => x.Employee).Include(x => x.Client)
                    .Include(x => x.Activity!).ThenInclude(x => x.Process)
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.Employee!.EmployeeCode).ThenBy(x => x.Client!.ClientCode).ThenBy(x => x.Activity!.ActivityName).ToList(),
                Clients = _context.Clients.Where(x => x.IsActive).OrderBy(x => x.ClientCode).ToList(),
                Employees = _context.Employees.Where(x => x.IsActive && x.Role != "SuperAdmin" && x.Role != "Viewer" && x.Role != "Editor").OrderBy(x => x.EmployeeCode).ToList(),
                Departments = _context.Departments.Where(x => x.IsActive).OrderBy(x => x.DepartmentName).ToList(),
                Groups = _context.ProductivityGroups.Where(x => x.IsActive).Include(x => x.Members).ThenInclude(x => x.Employee).OrderBy(x => x.GroupName).ToList(),
                GroupMembers = _context.ProductivityGroupMembers.Where(x => x.IsActive).Include(x => x.Employee).Include(x => x.Group).OrderBy(x => x.Group!.GroupName).ThenBy(x => x.Employee!.EmployeeCode).ToList(),
                GroupAssignments = _context.ProductivityGroupAssignments.Where(x => x.IsActive).Include(x => x.Group).Include(x => x.Client).Include(x => x.Activity!).ThenInclude(x => x.Process).OrderBy(x => x.Group!.GroupName).ThenBy(x => x.Activity!.ActivityName).ToList(),
                SelectedProcessId = processId
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateProcess(string processCode, string processName, int? clientId, int? departmentId)
        {
            if (!CanManageProductivity()) return Forbid();
            processCode = (processCode ?? string.Empty).Trim();
            processName = (processName ?? string.Empty).Trim();
            clientId = clientId == 0 ? null : clientId;

            if (string.IsNullOrWhiteSpace(processCode) || string.IsNullOrWhiteSpace(processName) ||
                (clientId.HasValue && !_context.Clients.Any(x => x.ClientId == clientId.Value && x.IsActive)))
            {
                TempData["Error"] = "Process code and process name are required. If a client is selected, it must be active.";
                return RedirectToAction(nameof(Setup));
            }

            if (_context.ProductivityProcesses.Any(x => x.ClientId == clientId && x.ProcessCode == processCode))
            {
                TempData["Error"] = clientId.HasValue
                    ? "That process code already exists for this client."
                    : "That global process code already exists for ALL CLIENTS.";
                return RedirectToAction(nameof(Setup));
            }

            _context.ProductivityProcesses.Add(new ProductivityProcess
            {
                ProcessCode = processCode,
                ProcessName = processName,
                ClientId = clientId,
                DepartmentId = departmentId,
                IsActive = true
            });
            _context.SaveChanges();
            TempData["Success"] = clientId.HasValue
                ? "Productivity process created."
                : "Global productivity process created for ALL CLIENTS.";
            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateActivity(int processId, string activityCode, string activityName, string unit, string? measurementMethod, decimal defaultTargetPerDay, decimal defaultWeight)
        {
            if (!CanManageProductivity()) return Forbid();
            var process = _context.ProductivityProcesses.FirstOrDefault(x => x.ProductivityProcessId == processId && x.IsActive);
            if (process == null) return NotFound();
            activityCode = (activityCode ?? string.Empty).Trim();
            activityName = (activityName ?? string.Empty).Trim();
            unit = (unit ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(activityCode) || string.IsNullOrWhiteSpace(activityName) || string.IsNullOrWhiteSpace(unit) || defaultTargetPerDay < 0 || defaultWeight <= 0)
            {
                TempData["Error"] = "Activity code, name, unit, target and weight must be valid.";
                return RedirectToAction(nameof(Setup), new { processId });
            }
            if (_context.ProductivityActivities.Any(x => x.ProductivityProcessId == processId && x.ActivityCode == activityCode))
            {
                TempData["Error"] = "That activity code already exists for this process.";
                return RedirectToAction(nameof(Setup), new { processId });
            }
            _context.ProductivityActivities.Add(new ProductivityActivity
            {
                ProductivityProcessId = processId,
                ActivityCode = activityCode,
                ActivityName = activityName,
                Unit = unit,
                MeasurementMethod = string.IsNullOrWhiteSpace(measurementMethod) ? null : measurementMethod.Trim(),
                DefaultTargetPerDay = defaultTargetPerDay,
                DefaultWeight = defaultWeight,
                IsActive = true
            });
            _context.SaveChanges();
            TempData["Success"] = "Productivity activity created.";
            return RedirectToAction(nameof(Setup), new { processId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateAssignment(int employeeId, int clientId, int activityId, decimal targetPerDay, decimal weight, DateTime effectiveFrom, DateTime? effectiveTo)
        {
            if (!CanManageProductivity()) return Forbid();
            var employee = _context.Employees.FirstOrDefault(x => x.EmployeeId == employeeId && x.IsActive);
            var activity = _context.ProductivityActivities.Include(x => x.Process).FirstOrDefault(x => x.ProductivityActivityId == activityId && x.IsActive);
            if (employee == null || activity == null || activity.Process == null ||
                (activity.Process.ClientId.HasValue && activity.Process.ClientId.Value != clientId))
            {
                TempData["Error"] = "Employee, client and activity combination is invalid.";
                return RedirectToAction(nameof(Setup));
            }
            if (targetPerDay < 0 || weight <= 0 || (effectiveTo.HasValue && effectiveTo.Value.Date < effectiveFrom.Date))
            {
                TempData["Error"] = "Target, weight and effective dates are invalid.";
                return RedirectToAction(nameof(Setup));
            }
            var effectiveToDate = effectiveTo.HasValue ? effectiveTo.Value.Date : DateTime.MaxValue.Date;
            var overlap = _context.ProductivityAssignments.Any(x => x.EmployeeId == employeeId && x.ClientId == clientId && x.ProductivityActivityId == activityId && x.IsActive && x.EffectiveFrom.Date <= effectiveToDate && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= effectiveFrom.Date));
            if (overlap)
            {
                TempData["Error"] = "An active assignment already overlaps this effective period.";
                return RedirectToAction(nameof(Setup));
            }
            _context.ProductivityAssignments.Add(new ProductivityAssignment
            {
                EmployeeId = employeeId,
                ClientId = clientId,
                ProductivityActivityId = activityId,
                TargetPerDay = targetPerDay,
                Weight = weight,
                EffectiveFrom = effectiveFrom.Date,
                EffectiveTo = effectiveTo?.Date,
                IsActive = true,
                CreatedBy = HttpContext.Session.GetString("EmployeeName")
            });
            _context.SaveChanges();
            TempData["Success"] = "Employee productivity assignment created.";
            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateGroup(string groupName, string? description)
        {
            if (!CanManageProductivity()) return Forbid();
            groupName = (groupName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(groupName))
            {
                TempData["Error"] = "Group name is required.";
                return RedirectToAction(nameof(Setup));
            }
            if (_context.ProductivityGroups.Any(x => x.GroupName == groupName))
            {
                TempData["Error"] = "That productivity group already exists.";
                return RedirectToAction(nameof(Setup));
            }
            _context.ProductivityGroups.Add(new ProductivityGroup
            {
                GroupName = groupName,
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                CreatedBy = HttpContext.Session.GetString("EmployeeName")
            });
            _context.SaveChanges();
            TempData["Success"] = "Productivity group created.";
            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveGroupMembers(int groupId, int[] employeeIds, DateTime effectiveFrom, DateTime? effectiveTo)
        {
            if (!CanManageProductivity()) return Forbid();
            var group = _context.ProductivityGroups.FirstOrDefault(x => x.ProductivityGroupId == groupId && x.IsActive);
            if (group == null) return NotFound();
            if (effectiveTo.HasValue && effectiveTo.Value.Date < effectiveFrom.Date)
            {
                TempData["Error"] = "Group membership dates are invalid.";
                return RedirectToAction(nameof(Setup));
            }

            var employees = _context.Employees
                .Where(x => employeeIds.Contains(x.EmployeeId) && x.IsActive && x.Role != "SuperAdmin" && x.Role != "Viewer" && x.Role != "Editor")
                .ToList();

            foreach (var employee in employees)
            {
                var overlap = _context.ProductivityGroupMembers.Any(x =>
                    x.ProductivityGroupId == groupId && x.EmployeeId == employee.EmployeeId && x.IsActive &&
                    x.EffectiveFrom.Date <= (effectiveTo.HasValue ? effectiveTo.Value.Date : DateTime.MaxValue.Date) &&
                    (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= effectiveFrom.Date));
                if (!overlap)
                {
                    _context.ProductivityGroupMembers.Add(new ProductivityGroupMember
                    {
                        ProductivityGroupId = groupId,
                        EmployeeId = employee.EmployeeId,
                        EffectiveFrom = effectiveFrom.Date,
                        EffectiveTo = effectiveTo?.Date,
                        IsActive = true
                    });
                }
            }
            _context.SaveChanges();
            TempData["Success"] = $"{employees.Count} employee(s) added to the productivity group.";
            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateGroupAssignment(int groupId, int clientId, int activityId, decimal targetPerDay, decimal weight, DateTime effectiveFrom, DateTime? effectiveTo)
        {
            if (!CanManageProductivity()) return Forbid();
            var group = _context.ProductivityGroups.FirstOrDefault(x => x.ProductivityGroupId == groupId && x.IsActive);
            var activity = _context.ProductivityActivities.Include(x => x.Process).FirstOrDefault(x => x.ProductivityActivityId == activityId && x.IsActive);
            if (group == null || activity?.Process == null)
            {
                TempData["Error"] = "Group and activity are required.";
                return RedirectToAction(nameof(Setup));
            }
            if (clientId != 0 && !_context.Clients.Any(x => x.ClientId == clientId && x.IsActive))
            {
                TempData["Error"] = "Selected client is invalid.";
                return RedirectToAction(nameof(Setup));
            }
            if (targetPerDay < 0 || weight <= 0 || (effectiveTo.HasValue && effectiveTo.Value.Date < effectiveFrom.Date))
            {
                TempData["Error"] = "Target, weight and effective dates are invalid.";
                return RedirectToAction(nameof(Setup));
            }
            var overlap = _context.ProductivityGroupAssignments.Any(x =>
                x.ProductivityGroupId == groupId && x.ClientId == (clientId == 0 ? null : clientId) &&
                x.ProductivityActivityId == activityId && x.IsActive &&
                x.EffectiveFrom.Date <= (effectiveTo.HasValue ? effectiveTo.Value.Date : DateTime.MaxValue.Date) &&
                (!x.EffectiveTo.HasValue || x.EffectiveTo.Value.Date >= effectiveFrom.Date));
            if (overlap)
            {
                TempData["Error"] = "That group/activity/client scope already has an overlapping effective assignment.";
                return RedirectToAction(nameof(Setup));
            }
            _context.ProductivityGroupAssignments.Add(new ProductivityGroupAssignment
            {
                ProductivityGroupId = groupId,
                ClientId = clientId == 0 ? null : clientId,
                ProductivityActivityId = activityId,
                TargetPerDay = targetPerDay,
                Weight = weight,
                EffectiveFrom = effectiveFrom.Date,
                EffectiveTo = effectiveTo?.Date,
                IsActive = true,
                CreatedBy = HttpContext.Session.GetString("EmployeeName")
            });
            _context.SaveChanges();
            TempData["Success"] = clientId == 0 ? "Group activity assigned to ALL CLIENTS." : "Group activity assignment created.";
            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeactivateGroupAssignment(int id)
        {
            if (!CanManageProductivity()) return Forbid();
            var item = _context.ProductivityGroupAssignments.Find(id);
            if (item == null) return NotFound();
            item.IsActive = false;
            if (!item.EffectiveTo.HasValue || item.EffectiveTo.Value.Date > DateTime.Today)
                item.EffectiveTo = DateTime.Today;
            _context.SaveChanges();
            TempData["Success"] = "Group assignment deactivated. Historical production remains unchanged.";
            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeactivateGroupMember(int id)
        {
            if (!CanManageProductivity()) return Forbid();
            var item = _context.ProductivityGroupMembers.Find(id);
            if (item == null) return NotFound();
            item.IsActive = false;
            if (!item.EffectiveTo.HasValue || item.EffectiveTo.Value.Date > DateTime.Today)
                item.EffectiveTo = DateTime.Today;
            _context.SaveChanges();
            TempData["Success"] = "Group member removed.";
            return RedirectToAction(nameof(Setup));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeactivateAssignment(int id)
        {
            if (!CanManageProductivity()) return Forbid();
            var item = _context.ProductivityAssignments.Find(id);
            if (item == null) return NotFound();
            item.IsActive = false;
            if (!item.EffectiveTo.HasValue || item.EffectiveTo.Value.Date > DateTime.Today)
                item.EffectiveTo = DateTime.Today;
            _context.SaveChanges();
            TempData["Success"] = "Assignment deactivated. Historical entries remain unchanged.";
            return RedirectToAction(nameof(Setup));
        }

        //==================================================
        // Productivity Register
        //==================================================
        [HttpGet]
        public IActionResult Register(int? employeeId, int? clientId, DateTime? date)
        {
            if (!CanViewProductivity()) return Forbid();

            var current = _reportingService.GetCurrentEmployee();
            if (current == null) return RedirectToAction("Login", "Account");

            var visibleEmployees = GetEntryEmployees(current);
            var selectedEmployee = employeeId ?? current.EmployeeId;
            if (!visibleEmployees.Any(x => x.EmployeeId == selectedEmployee))
                return Forbid();

            var selectedDate = date?.Date ?? DateTimeHelper.GetIST().Date;
            var effectiveAssignments = _productivityService.GetEffectiveAssignments(selectedEmployee, selectedDate);
            var clients = effectiveAssignments
                .Where(x => x.Client != null)
                .Select(x => x.Client!)
                .GroupBy(x => x.ClientId).Select(g => g.First())
                .OrderBy(x => x.ClientCode).ToList();

            if (clientId.HasValue && clients.All(x => x.ClientId != clientId.Value)) clientId = null;

            var initialClient = clientId ?? clients.FirstOrDefault()?.ClientId ?? 0;
            var initialActivity = effectiveAssignments
                .Where(x => x.ClientId == initialClient && x.Activity != null)
                .Select(x => x.Activity!)
                .GroupBy(x => x.ProductivityActivityId).Select(g => g.First())
                .OrderBy(x => x.ActivityName)
                .FirstOrDefault();

            var vm = new ProductivityRegisterViewModel
            {
                ProductionDate = selectedDate,
                EmployeeId = selectedEmployee,
                ClientId = initialClient,
                ProductivityActivityId = initialActivity?.ProductivityActivityId ?? 0,
                Quantity = 0,
                Employees = visibleEmployees,
                Clients = clients,
                Activities = effectiveAssignments.Where(x => x.Activity != null).Select(x => x.Activity!).GroupBy(x => x.ProductivityActivityId).Select(g => g.First()).OrderBy(x => x.ActivityName).ToList(),
                AssignmentOptions = effectiveAssignments
                    .Where(x => x.Activity != null && x.Client != null)
                    .Select(x => new ProductivityRegisterAssignmentOption
                    {
                        ClientId = x.ClientId,
                        ActivityId = x.ProductivityActivityId,
                        ClientCode = x.Client!.ClientCode,
                        ClientName = x.Client.ClientName,
                        ProcessName = x.Activity!.Process?.ProcessName ?? string.Empty,
                        ActivityName = x.Activity.ActivityName,
                        Unit = x.Activity.Unit,
                        TargetPerDay = x.TargetPerDay,
                        Weight = x.Weight
                    })
                    .GroupBy(x => new { x.ClientId, x.ActivityId })
                    .Select(g => g.First())
                    .OrderBy(x => x.ClientCode).ThenBy(x => x.ProcessName).ThenBy(x => x.ActivityName)
                    .ToList(),
                Lines = new List<ProductivityRegisterLineViewModel>
                {
                    new ProductivityRegisterLineViewModel
                    {
                        ClientId = initialClient,
                        ProductivityActivityId = initialActivity?.ProductivityActivityId ?? 0,
                        Quantity = 0
                    }
                },
                RecentEntries = _context.ProductivityEntries
                    .Include(x => x.Employee).Include(x => x.Client).Include(x => x.Activity!).ThenInclude(a => a.Process)
                    .Where(x => x.ProductionDate.Date == selectedDate && visibleEmployees.Select(e => e.EmployeeId).Contains(x.EmployeeId))
                    .OrderByDescending(x => x.EnteredOn).Take(50).ToList()
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(ProductivityRegisterViewModel model)
        {
            if (!CanViewProductivity()) return Forbid();

            var current = _reportingService.GetCurrentEmployee();
            if (current == null) return RedirectToAction("Login", "Account");

            var visible = GetEntryEmployees(current);
            if (!visible.Any(x => x.EmployeeId == model.EmployeeId)) return Forbid();

            var lines = (model.Lines ?? new List<ProductivityRegisterLineViewModel>())
                .Where(x => x.ClientId > 0 || x.ProductivityActivityId > 0 || x.Quantity > 0 || !string.IsNullOrWhiteSpace(x.Remarks))
                .ToList();

            if (lines.Count == 0)
            {
                TempData["Error"] = "Add at least one production activity.";
                return RedirectToAction(nameof(Register), new { employeeId = model.EmployeeId, date = model.ProductionDate.ToString("yyyy-MM-dd") });
            }

            var assignments = new List<(ProductivityRegisterLineViewModel Line, ProductivityAssignment Assignment)>();
            foreach (var line in lines)
            {
                if (line.ClientId <= 0 || line.ProductivityActivityId <= 0 || line.Quantity < 0)
                {
                    TempData["Error"] = "Every activity row must have a client, activity and a non-negative quantity.";
                    return RedirectToAction(nameof(Register), new { employeeId = model.EmployeeId, date = model.ProductionDate.ToString("yyyy-MM-dd") });
                }

                var assignment = _productivityService.FindAssignment(model.EmployeeId, line.ClientId, line.ProductivityActivityId, model.ProductionDate);
                if (assignment == null)
                {
                    TempData["Error"] = "One or more activities are not assigned to this employee/client for the selected date.";
                    return RedirectToAction(nameof(Register), new { employeeId = model.EmployeeId, date = model.ProductionDate.ToString("yyyy-MM-dd") });
                }

                assignments.Add((line, assignment));
            }

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                foreach (var item in assignments)
                {
                    var line = item.Line;
                    var assignment = item.Assignment;
                    _context.ProductivityEntries.Add(new ProductivityEntry
                    {
                        ProductionDate = model.ProductionDate.Date,
                        EmployeeId = model.EmployeeId,
                        ClientId = line.ClientId,
                        ProductivityActivityId = line.ProductivityActivityId,
                        Quantity = line.Quantity,
                        TargetPerDaySnapshot = assignment.TargetPerDay,
                        WeightSnapshot = assignment.Weight,
                        WeightedAchievement = line.Quantity * assignment.Weight,
                        Remarks = string.IsNullOrWhiteSpace(line.Remarks) ? null : line.Remarks.Trim(),
                        EnteredByEmployeeId = current.EmployeeId,
                        EnteredOn = DateTime.Now,
                        Status = "Final"
                    });
                }

                _context.SaveChanges();
                transaction.Commit();
                TempData["Success"] = $"{assignments.Count} production activity/activities saved successfully.";
            }
            catch
            {
                transaction.Rollback();
                TempData["Error"] = "The production entries could not be saved. No partial entries were committed.";
            }

            return RedirectToAction(nameof(Register), new { employeeId = model.EmployeeId, date = model.ProductionDate.ToString("yyyy-MM-dd") });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10_000_000)]
        public IActionResult UploadSpreadsheet(int employeeId, DateTime productionDate, IFormFile? productionFile)
        {
            if (!CanViewProductivity()) return Forbid();

            var current = _reportingService.GetCurrentEmployee();
            if (current == null) return RedirectToAction("Login", "Account");
            if (!GetEntryEmployees(current).Any(x => x.EmployeeId == employeeId)) return Forbid();

            if (productionFile == null || productionFile.Length == 0)
            {
                TempData["Error"] = "Please select the employee production spreadsheet to upload.";
                return RedirectToAction(nameof(Register), new { employeeId, date = productionDate.ToString("yyyy-MM-dd") });
            }

            var extension = Path.GetExtension(productionFile.FileName).ToLowerInvariant();
            var allowed = new[] { ".xlsx", ".xls", ".csv" };
            if (!allowed.Contains(extension))
            {
                TempData["Error"] = "Only Excel (.xlsx/.xls) or CSV production spreadsheets are allowed.";
                return RedirectToAction(nameof(Register), new { employeeId, date = productionDate.ToString("yyyy-MM-dd") });
            }

            var employee = _context.Employees.FirstOrDefault(x => x.EmployeeId == employeeId && x.IsActive);
            if (employee == null) return NotFound();

            var root = Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "ProductivityUploads", employee.EmployeeCode, productionDate.Date.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(root);

            var safeName = Path.GetFileName(productionFile.FileName);
            var storedName = $"{DateTime.Now:HHmmss}_{Guid.NewGuid():N}_{safeName}";
            var destination = Path.Combine(root, storedName);
            using (var stream = new FileStream(destination, FileMode.CreateNew))
            {
                productionFile.CopyTo(stream);
            }

            TempData["Success"] = $"Production spreadsheet uploaded for {employee.EmployeeCode} for {productionDate:dd-MMM-yyyy}.";
            return RedirectToAction(nameof(Register), new { employeeId, date = productionDate.ToString("yyyy-MM-dd") });
        }

        //==================================================
        // Productivity Reports
        //==================================================
        [HttpGet]
        public IActionResult Reports(DateTime? fromDate, DateTime? toDate, int? employeeId, int? clientId, int? processId)
        {
            if (!CanViewProductivity()) return Forbid();
            var current = _reportingService.GetCurrentEmployee();
            if (current == null) return RedirectToAction("Login", "Account");
            var payroll = _productivityService.GetCurrentPayrollCycle();
            var from = fromDate?.Date ?? payroll?.PeriodStart.Date ?? DateTimeHelper.GetIST().Date;
            var to = toDate?.Date ?? payroll?.PeriodEnd.Date ?? DateTimeHelper.GetIST().Date;
            var visible = GetEntryEmployees(current);
            if (employeeId.HasValue && !visible.Any(x => x.EmployeeId == employeeId.Value)) return Forbid();

            var vm = new ProductivityReportsViewModel
            {
                FromDate = from,
                ToDate = to,
                EmployeeId = employeeId,
                ClientId = clientId,
                ProcessId = processId,
                Employees = visible,
                Clients = _context.Clients.Where(x => x.IsActive).OrderBy(x => x.ClientCode).ToList(),
                Processes = _context.ProductivityProcesses.Where(x => x.IsActive).OrderBy(x => x.ProcessName).ToList(),
                Rows = _productivityService.BuildReport(from, to, employeeId, clientId, processId)
                    .Where(x => visible.Any(e => e.EmployeeId == x.EmployeeId)).ToList()
            };
            return View(vm);
        }

        [HttpGet]
        public IActionResult ExportReport(DateTime? fromDate, DateTime? toDate, int? employeeId, int? clientId, int? processId)
        {
            if (!CanViewProductivity()) return Forbid();
            var current = _reportingService.GetCurrentEmployee();
            if (current == null) return Unauthorized();
            var visible = GetEntryEmployees(current);
            if (employeeId.HasValue && !visible.Any(x => x.EmployeeId == employeeId.Value)) return Forbid();
            var payroll = _productivityService.GetCurrentPayrollCycle();
            var rows = _productivityService.BuildReport(
                fromDate?.Date ?? payroll?.PeriodStart.Date ?? DateTimeHelper.GetIST().Date,
                toDate?.Date ?? payroll?.PeriodEnd.Date ?? DateTimeHelper.GetIST().Date,
                employeeId, clientId, processId)
                .Where(x => visible.Any(e => e.EmployeeId == x.EmployeeId)).ToList();
            var sb = new StringBuilder();
            sb.AppendLine("Employee Code,Employee Name,Client Code,Client Name,Process,Activity,Target,Actual,Weighted Achievement,Achievement %");
            foreach (var x in rows)
                sb.AppendLine(string.Join(",", Csv(x.EmployeeCode), Csv(x.EmployeeName), Csv(x.ClientCode), Csv(x.ClientName), Csv(x.ProcessName), Csv(x.ActivityName), x.Target, x.Actual, x.WeightedAchievement, x.AchievementPercent.ToString("0.00")));
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"Productivity_{DateTime.Today:yyyyMMdd}.csv");
        }

        private List<Employee> GetEntryEmployees(Employee current)
        {
            return _reportingService.GetVisibleEmployees()
                .OrderBy(x => x.EmployeeCode)
                .ToList();
        }

        private bool CanViewProductivity()
        {
            var role = HttpContext.Session.GetString("Role")?.Trim();
            return string.Equals(role, "Employee", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(role, "Team Leader", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
        }

        private bool CanManageProductivity()
        {
            var role = HttpContext.Session.GetString("Role")?.Trim();
            return string.Equals(role, "Manager", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
        }

        private static string Csv(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
    }
}

