using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FidelitasHub.Models
{
    public class ProductivityActivity
    {
        [Key]
        public int ProductivityActivityId { get; set; }

        public int ProductivityProcessId { get; set; }

        [ForeignKey(nameof(ProductivityProcessId))]
        public ProductivityProcess? Process { get; set; }

        [Required, MaxLength(50)]
        public string ActivityCode { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string ActivityName { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string Unit { get; set; } = "Count";

        [MaxLength(100)]
        public string? MeasurementMethod { get; set; }

        public decimal DefaultTargetPerDay { get; set; }
        public decimal DefaultWeight { get; set; } = 1m;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public DateTime? ModifiedOn { get; set; }
    }
}
