using FidelitasHub.Data;
using FidelitasHub.Models;
using FidelitasHub.ViewModels;

namespace FidelitasHub.Services.Configuration
{
    public class ConfigurationService
    {
        private readonly ApplicationDbContext _context;

        public ConfigurationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public string GetValue(string key)
        {
            return _context.SystemSettings
                .FirstOrDefault(x => x.SettingKey == key)?
                .SettingValue ?? "";
        }

        public void SetValue(string key, string value)
        {
            var setting = _context.SystemSettings
                .FirstOrDefault(x => x.SettingKey == key);

            if (setting == null)
                return;

            setting.SettingValue = value;

            _context.SaveChanges();
        }

        //==================================================
        // Email Settings
        //==================================================

        public EmailSettingsViewModel GetEmailSettings()
        {
            return new EmailSettingsViewModel
            {
                SmtpServer = GetValue("SMTP_SERVER"),

                Port = int.TryParse(GetValue("SMTP_PORT"), out int port)
                    ? port
                    : 465,

                Username = GetValue("SMTP_USERNAME"),

                Password = GetValue("SMTP_PASSWORD"),

                EnableSSL = GetValue("SMTP_SSL") == "true",

                SenderName = GetValue("SENDER_NAME"),

                SenderEmail = GetValue("SENDER_EMAIL")
            };
        }

        //==================================================
        // Save Email Settings
        //==================================================

        public void SaveEmailSettings(EmailSettingsViewModel model)
        {
            SetValue("SMTP_SERVER", model.SmtpServer);

            SetValue("SMTP_PORT", model.Port.ToString());

            SetValue("SMTP_USERNAME", model.Username);

            SetValue("SMTP_PASSWORD", model.Password);

            SetValue("SMTP_SSL",
                model.EnableSSL ? "true" : "false");

            SetValue("SENDER_NAME", model.SenderName);

            SetValue("SENDER_EMAIL", model.SenderEmail);
        }
    }
}