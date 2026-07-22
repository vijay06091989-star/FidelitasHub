using System;
using System.ComponentModel.DataAnnotations;

namespace FidelitasHub.Models
{
    public class Holiday
    {
        [Key]
        public int HolidayId { get; set; }

        [Required]
        [Display(Name = "Holiday Name")]
        [StringLength(100)]
        public string HolidayName { get; set; }

        [Required]
        [Display(Name = "Holiday Date")]
        [DataType(DataType.Date)]
        public DateTime HolidayDate { get; set; }

        [Required]
        [Display(Name = "Type")]
        [StringLength(30)]
        public string HolidayType { get; set; } = "National";

        [Display(Name = "Description")]
        [StringLength(250)]
        public string? Description { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
