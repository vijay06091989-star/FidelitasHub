using FidelitasHub.Data;
using FidelitasHub.Frontend.ViewModels.Email;
using FidelitasHub.Services.Email;
using FidelitasHub.Services.Email.Builders;
using FidelitasHub.ViewModels.Email;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FidelitasHub.Controllers
{
    public class EmployeeEmailController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailTemplateService _emailTemplateService;
        private readonly EmailService _emailService;

        public EmployeeEmailController(
    ApplicationDbContext context,
    EmailTemplateService emailTemplateService,
    EmailService emailService)
        {
            _context = context;
            _emailTemplateService = emailTemplateService;
            _emailService = emailService;
        }

        //====================================================
        // Employee Email
        //====================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            SendEmployeeEmailViewModel model = new();

            await LoadDropDowns(model);

            return View(model);
        }

        //====================================================
        // Preview
        //====================================================

        [HttpPost]
        public async Task<IActionResult> Preview(SendEmployeeEmailViewModel model)
        {
            await LoadDropDowns(model);

            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            var employee = await _context.Employees
    .FirstOrDefaultAsync(x => x.EmployeeId == model.EmployeeId);

            if (employee == null)
            {
                return NotFound();
            }

            var values = EmployeeEmailData.Create(
                employee.EmployeeName,
                employee.EmployeeCode,
                employee.Department,
                employee.Designation,
                "Reporting Manager",
                "Fidelitas Healthcare Pvt Ltd");

            var result = await _emailTemplateService.RenderTemplateAsync(
                model.EmailTemplateId,
                values);

            var preview = new EmployeeEmailPreviewViewModel
            {
                EmployeeId = employee.EmployeeId,
                TemplateId = model.EmailTemplateId,
                EmployeeName = employee.EmployeeName,
                EmailAddress = employee.Email,
                Subject = result.Subject,
                Body = result.Body
            };

            return View("Preview", preview);
        }

        //====================================================
        // Send Email
        //====================================================

        [HttpPost]
        public async Task<IActionResult> Send(EmployeeEmailPreviewViewModel model)
        {
            var employee = await _context.Employees
                .FirstOrDefaultAsync(x => x.EmployeeId == model.EmployeeId);

            if (employee == null)
            {
                TempData["Error"] = "Employee not found.";

                return RedirectToAction(nameof(Index));
            }

            var values = EmployeeEmailData.Create(
    employee.EmployeeName,
    employee.EmployeeCode,
    employee.Department,
    employee.Designation,
    "Reporting Manager",
    "Fidelitas Healthcare Pvt Ltd");

            var rendered = await _emailTemplateService.RenderTemplateAsync(
    model.TemplateId,
    values);

            await _emailService.SendEmailAsync(
                employee.Email,
                rendered.Subject,
                rendered.Body,
                true);

            TempData["Success"] = $"Email sent successfully to {employee.EmployeeName}.";

            return RedirectToAction(nameof(Index));
        }

        //====================================================
        // Load Drop Downs
        //====================================================

        private async Task LoadDropDowns(SendEmployeeEmailViewModel model)
        {
            model.Employees = await _context.Employees
                .Where(x => x.IsActive)
                .OrderBy(x => x.EmployeeName)
                .Select(x => new SelectListItem
                {
                    Value = x.EmployeeId.ToString(),
                    Text = x.EmployeeCode + " - " + x.EmployeeName
                })
                .ToListAsync();

            model.EmailTemplates = await _context.EmailTemplates
                .Where(x => x.IsActive)
                .OrderBy(x => x.TemplateName)
                .Select(x => new SelectListItem
                {
                    Value = x.Id.ToString(),
                    Text = x.TemplateName
                })
                .ToListAsync();
        }
    }
}