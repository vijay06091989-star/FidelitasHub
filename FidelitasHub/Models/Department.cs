using System;
using System.ComponentModel.DataAnnotations;

namespace FidelitasHub.Models
{
    public class Department
    {
        [Key]
        public int DepartmentId { get; set; }

        [Required]
        [Display(Name = "Department Code")]
        [StringLength(10)]
        public string DepartmentCode { get; set; }

        [Required]
        [Display(Name = "Department Name")]
        [StringLength(100)]
        public string DepartmentName { get; set; }

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