using System;
using System.ComponentModel.DataAnnotations;

namespace FidelitasHub.Models
{
    public class LeaveType
    {
        [Key]
        public int LeaveTypeId { get; set; }

        [Required]
        [Display(Name = "Leave Type Code")]
        [StringLength(10)]
        public string LeaveTypeCode { get; set; }

        [Required]
        [Display(Name = "Leave Type Name")]
        [StringLength(100)]
        public string LeaveTypeName { get; set; }

        [Required]
        [Display(Name = "Short Code")]
        [StringLength(10)]
        public string ShortCode { get; set; }

        [Display(Name = "Default Annual Days")]
        [Range(0, 365)]
        public decimal DefaultAnnualDays { get; set; }

        [Display(Name = "Paid Leave")]
        public bool IsPaid { get; set; } = true;

        [Display(Name = "Description")]
        [StringLength(250)]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Modified Date")]
        public DateTime? ModifiedDate { get; set; }
    }
}
