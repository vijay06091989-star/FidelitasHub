using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FidelitasHub.Models
{
    public class ProductivityAssignment
    {
        [Key]
        public int ProductivityAssignmentId { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        public int ClientId { get; set; }
        [ForeignKey(nameof(ClientId))]
        public Client? Client { get; set; }

        public int ProductivityActivityId { get; set; }
        [ForeignKey(nameof(ProductivityActivityId))]
        public ProductivityActivity? Activity { get; set; }

        public decimal TargetPerDay { get; set; }
        public decimal Weight { get; set; } = 1m;

        [Required]
        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;
        [MaxLength(200)]
        public string? CreatedBy { get; set; }
    }
}
