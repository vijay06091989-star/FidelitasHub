using FidelitasHub.Services.Email;
using FidelitasHub.ViewModels.Email;
using Microsoft.AspNetCore.Mvc;

namespace FidelitasHub.Controllers
{
    public class EmailTemplateController : Controller
    {
        private readonly EmailTemplateService _templateService;
        private readonly EmailService _emailService;

        public EmailTemplateController(
            EmailTemplateService templateService,
            EmailService emailService)
        {
            _templateService = templateService;
            _emailService = emailService;
        }

        //==================================================
        // Email Template Editor
        //==================================================

        public async Task<IActionResult> Index(int? id)
        {
            var templates = await _templateService.GetAllAsync();

            if (!templates.Any())
            {
                return View(new EmailTemplateEditorViewModel());
            }

            var selectedTemplate = id.HasValue
                ? await _templateService.GetByIdAsync(id.Value)
                : templates.First();

            var vm = new EmailTemplateEditorViewModel
            {
                Templates = templates,
                SelectedTemplate = selectedTemplate,
                Placeholders = EmailPlaceholders.GetAll()
            };

            return View(vm);
        }

        //==================================================
        // Save Template
        //==================================================

        [HttpPost]
        public async Task<IActionResult> Save(
            [FromBody] EmailTemplateSaveViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var template = await _templateService.GetByIdAsync(model.Id);

            if (template == null)
            {
                return NotFound();
            }

            template.Subject = model.Subject;
            template.Body = model.Body;
            template.Category = model.Category;

            await _templateService.SaveAsync(template);

            return Ok(new
            {
                success = true,
                message = "Template saved successfully."
            });
        }

        //==================================================
        // Send Test Email
        //==================================================

        [HttpPost]
        public async Task<IActionResult> SendTest(
            [FromBody] SendTestEmailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var placeholders = EmailSampleData.Get();

            var rendered = await _templateService.RenderTemplateAsync(
                model.TemplateId,
                placeholders);

            await _emailService.SendEmailAsync(
                model.Email,
                rendered.Subject,
                rendered.Body);

            return Ok(new
            {
                success = true,
                message = $"Test email sent successfully to {model.Email}"
            });
        }

        //==================================================
        // Preview Template
        //==================================================

        [HttpPost]
        public IActionResult Preview(string subject, string body)
        {
            ViewBag.Subject = subject;
            ViewBag.Body = body;

            return View();
        }
    }
}