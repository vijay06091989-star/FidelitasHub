using FidelitasHub.Models;

namespace FidelitasHub.ViewModels.Email
{
    public class EmailTemplateEditorViewModel
    {
        public List<EmailTemplate> Templates { get; set; } = new();

        public EmailTemplate? SelectedTemplate { get; set; }

        public List<string> Placeholders { get; set; } = new();
    }
}