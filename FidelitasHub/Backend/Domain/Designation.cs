using System;
using System.ComponentModel.DataAnnotations;

namespace FidelitasHub.Models
{
    public class Designation
    {
        [Key]
        public int DesignationId { get; set; }

        [Required]
        [Display(Name = "Designation Code")]
        [StringLength(10)]
        public string DesignationCode { get; set; }

        [Required]
        [Display(Name = "Designation Name")]
        [StringLength(100)]
        public string DesignationName { get; set; }

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
