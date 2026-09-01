using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace FidelitasHub.Models
{
    public class LeaveBalanceImportViewModel
    {
        [Required]
        public IFormFile? ExcelFile { get; set; }


        [Required(ErrorMessage = "Please select payroll cycle.")]
        public int PayrollCalendarId { get; set; }


        public List<PayrollCalendar> PayrollCycles { get; set; }
            = new List<PayrollCalendar>();


        public List<LeaveBalancePreview> PreviewData { get; set; }
            = new List<LeaveBalancePreview>();
    }
}