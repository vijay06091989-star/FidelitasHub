using Microsoft.AspNetCore.Mvc.Rendering;

namespace FidelitasHub.Frontend.ViewModels.Email
{
    public class SendEmployeeEmailViewModel
    {
        public int EmployeeId { get; set; }

        public int EmailTemplateId { get; set; }

        public List<SelectListItem> Employees { get; set; } = new();

        public List<SelectListItem> EmailTemplates { get; set; } = new();
    }
}