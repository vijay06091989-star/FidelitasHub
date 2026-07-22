using System;
using System.ComponentModel.DataAnnotations;

namespace FidelitasHub.Models
{
    public class Shift
    {
        [Key]
        public int ShiftId { get; set; }

        [Required]
        [StringLength(10)]
        [Display(Name = "Shift Code")]
        public string ShiftCode { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Shift Name")]
        public string ShiftName { get; set; }

        [Required]
        [Display(Name = "Standard Start Time")]
        public TimeSpan StandardStartTime { get; set; }

        [Required]
        [Display(Name = "Standard End Time")]
        public TimeSpan StandardEndTime { get; set; }

        [Display(Name = "DST Applicable")]
        public bool DstApplicable { get; set; }

        [Display(Name = "DST Start Time")]
        public TimeSpan? DstStartTime { get; set; }

        [Display(Name = "DST End Time")]
        public TimeSpan? DstEndTime { get; set; }

        [Display(Name = "Minimum Punch-In Time (Minutes Before Shift Start)")]
        public int MinimumPunchInMinutes { get; set; } = 30;

        [Display(Name = "Grace Minutes (Late Arrival)")]
        public int GraceMinutes { get; set; } = 15;

        [Display(Name = "Maximum Break Time (Minutes)")]
        public int MaximumBreakMinutes { get; set; } = 60;

        [Display(Name = "Maximum Punch-Out Time (Minutes After Shift End)")]
        public int MaximumPunchOutMinutes { get; set; } = 30;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}