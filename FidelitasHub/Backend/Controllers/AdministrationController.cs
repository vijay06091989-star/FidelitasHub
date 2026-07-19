using FidelitasHub.Data;
using Microsoft.AspNetCore.Mvc;

namespace FidelitasHub.Controllers
{
    public class AdministrationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdministrationController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Admin" || role == "SuperAdmin";
        }

        // Users -> reuses the Employee records (login accounts)
        public IActionResult Users(string searchText)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var users = _context.Employees.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                users = users.Where(e =>
                    e.EmployeeCode.Contains(searchText) ||
                    e.EmployeeName.Contains(searchText) ||
                    e.Role.Contains(searchText));
            }

            ViewBag.SearchText = searchText;
            return View(users.OrderBy(e => e.EmployeeCode).ToList());
        }

        // Roles -> derived from the roles in use, with member counts
        public IActionResult Roles()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var known = new[] { "SuperAdmin", "Admin", "HR", "Manager", "TeamLead", "Employee" };

            var counts = _context.Employees
                .GroupBy(e => e.Role)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToDictionary(x => x.Role ?? "", x => x.Count);

            ViewBag.RoleCounts = counts;
            return View(known);
        }

        public IActionResult CompanySettings()
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");
            return View();
        }

        // Audit Logs -> real data from the AuditLog table
        public IActionResult AuditLogs(string searchText)
        {
            if (!IsAdmin()) return RedirectToAction("Index", "Home");

            var logs = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                logs = logs.Where(a =>
                    a.Action.Contains(searchText) ||
                    a.Remarks.Contains(searchText));
            }

            ViewBag.EmployeeNames = _context.Employees
                .ToDictionary(e => e.EmployeeId, e => e.EmployeeName);
            ViewBag.SearchText = searchText;

            return View(logs.OrderByDescending(a => a.ActionTime).Take(500).ToList());
        }
    }
}
