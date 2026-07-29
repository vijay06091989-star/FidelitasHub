using Microsoft.AspNetCore.Mvc.Rendering;

namespace FidelitasHub.Frontend.ViewModels.Email
{
    public class BulkEmployeeEmailViewModel
    {
        public List<int> EmployeeIds { get; set; } = new();

        public int EmailTemplateId { get; set; }

        public List<SelectListItem> Employees { get; set; } = new();

        public List<SelectListItem> EmailTemplates { get; set; } = new();

        // Employee list for display
        public List<EmployeeSelectionViewModel> EmployeeList { get; set; } = new();
    }

    public class EmployeeSelectionViewModel
    {
        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; } = "";

        public string EmployeeName { get; set; } = "";
    }
}