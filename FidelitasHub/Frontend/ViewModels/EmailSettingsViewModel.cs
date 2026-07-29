using System.ComponentModel.DataAnnotations;

namespace FidelitasHub.ViewModels
{
    public class EmailSettingsViewModel
    {
        [Required]
        public string SmtpServer { get; set; } = string.Empty;

        [Required]
        public int Port { get; set; }

        [Required]
        [EmailAddress]
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public bool EnableSSL { get; set; }

        [Required]
        public string SenderName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string SenderEmail { get; set; } = string.Empty;
    }
}