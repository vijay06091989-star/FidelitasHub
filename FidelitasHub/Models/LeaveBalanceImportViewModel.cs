using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace FidelitasHub.Models
{
    public class LeaveBalanceImportViewModel
    {
        [Required]
        public IFormFile? ExcelFile { get; set; }

        public List<LeaveBalancePreview> PreviewData { get; set; }
            = new List<LeaveBalancePreview>();
    }
}