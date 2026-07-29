namespace FidelitasHub.ViewModels.Email
{
    public class EmployeeEmailPreviewViewModel
    {
        public int EmployeeId { get; set; }

        public int TemplateId { get; set; }

        public string EmployeeName { get; set; } = "";

        public string EmailAddress { get; set; } = "";

        public string Subject { get; set; } = "";

        public string Body { get; set; } = "";
    }
}