using System.ComponentModel.DataAnnotations;

namespace FidelitasHub.ViewModels.Email
{
    public class SendTestEmailViewModel
    {
        [Required]
        public int TemplateId { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}