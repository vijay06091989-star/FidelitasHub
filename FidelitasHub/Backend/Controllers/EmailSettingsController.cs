using FidelitasHub.Services.Configuration;
using FidelitasHub.Services.Email;
using FidelitasHub.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FidelitasHub.Controllers
{
    public class EmailSettingsController : Controller
    {
        private readonly ConfigurationService _configuration;
        private readonly EmailService _emailService;

        public EmailSettingsController(
            ConfigurationService configuration,
            EmailService emailService)
        {
            _configuration = configuration;
            _emailService = emailService;
        }

        //==================================================
        // Display Email Settings
        //==================================================

        [HttpGet]
        public IActionResult Index()
        {
            var model = _configuration.GetEmailSettings();

            return View(model);
        }

        //==================================================
        // Save Email Settings
        //==================================================

        [HttpPost]
        public IActionResult Index(EmailSettingsViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            _configuration.SaveEmailSettings(model);

            ViewBag.Success = "Email settings saved successfully.";

            return View(model);
        }

        //==================================================
        // Send Test Email
        //==================================================

        [HttpPost]
        public async Task<IActionResult> SendTestEmail(EmailSettingsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Error = string.Join("<br>",
                    ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));

                return View("Index", model);
            }

            _configuration.SaveEmailSettings(model);

            try
            {
                await _emailService.SendEmailAsync(
                    model.SenderEmail,
                    "Fidelitas Hub Test Email",
                    "<h2>Congratulations!</h2><p>Your email settings are working correctly.</p>");

                ViewBag.Success = "Test email sent successfully.";
            }
            catch (Exception ex)
            {
                ViewBag.Error =
                    ex.GetType().FullName + "<br/><br/>" +
                    ex.Message + "<br/><br/>" +
                    ex.StackTrace;
            }

            return View("Index", model);
        }
    }
}