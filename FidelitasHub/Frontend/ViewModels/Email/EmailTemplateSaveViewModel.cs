namespace FidelitasHub.ViewModels.Email
{
    public class EmailTemplateSaveViewModel
    {
        public int Id { get; set; }

        public string Subject { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;
    }
}