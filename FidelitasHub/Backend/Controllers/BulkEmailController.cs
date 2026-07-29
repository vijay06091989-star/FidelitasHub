using FidelitasHub.Data;
using FidelitasHub.Frontend.ViewModels.Email;
using FidelitasHub.Services.Email;
using FidelitasHub.Services.Email.Builders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FidelitasHub.Controllers
{
    public class BulkEmailController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly EmailTemplateService _templateService;

        public BulkEmailController(
            ApplicationDbContext context,
            EmailTemplateService emailTemplateService,
            EmailService emailService)
        {
            _context = context;
            _templateService = emailTemplateService;
            _emailService = emailService;
        }

        //====================================================
        // Bulk Email
        //====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            BulkEmployeeEmailViewModel model = new();

            model.EmployeeList = await _context.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.EmployeeName)
                .Select(e => new EmployeeSelectionViewModel
                {
                    EmployeeId = e.EmployeeId,
                    EmployeeCode = e.EmployeeCode,
                    EmployeeName = e.EmployeeName
                })
                .ToListAsync();

            model.EmailTemplates = await _context.EmailTemplates
                .Where(t => t.IsActive)
                .OrderBy(t => t.TemplateName)
                .Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = t.TemplateName
                })
                .ToListAsync();

            return View(model);
        }

        //====================================================
        // Send Bulk Email
        //====================================================

        [HttpPost]
        public async Task<IActionResult> Send(BulkEmployeeEmailViewModel model)
        {
            if (model.EmailTemplateId == 0)
            {
                TempData["Error"] = "Please select an email template.";
                return RedirectToAction(nameof(Index));
            }

            if (model.EmployeeIds == null || !model.EmployeeIds.Any())
            {
                TempData["Error"] = "Please select at least one employee.";
                return RedirectToAction(nameof(Index));
            }

            var employees = await _context.Employees
    .Where(e => model.EmployeeIds.Contains(e.EmployeeId))
    .ToListAsync();

            int selectedCount = employees.Count;
            int sentCount = 0;
            int failedCount = 0;
            int skippedCount = 0;

            List<string> failedEmployees = new();

            foreach (var employee in employees)
            {
                if (string.IsNullOrWhiteSpace(employee.Email))
                {
                    skippedCount++;
                    continue;
                }

                try
                {
                    var values = EmployeeEmailData.Create(
                        employee.EmployeeName,
                        employee.EmployeeCode,
                        employee.Department,
                        employee.Designation,
                        "Reporting Manager",
                        "Fidelitas Healthcare Pvt Ltd");

                    var rendered = await _templateService.RenderTemplateAsync(
                        model.EmailTemplateId,
                        values);

                    await _emailService.SendEmailAsync(
                        employee.Email,
                        rendered.Subject,
                        rendered.Body,
                        true);

                    sentCount++;
                }
                catch
                {
                    failedCount++;
                    failedEmployees.Add($"{employee.EmployeeName} ({employee.Email})");
                }
            }

            TempData["Success"] =
    $"Bulk Email Completed\n\n" +
    $"Selected : {selectedCount}\n" +
    $"Sent : {sentCount}\n" +
    $"Failed : {failedCount}\n" +
    $"Skipped : {skippedCount}";

            if (failedEmployees.Any())
            {
                TempData["FailedEmployees"] = string.Join("<br/>", failedEmployees);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}