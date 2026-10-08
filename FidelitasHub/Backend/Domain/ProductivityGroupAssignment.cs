using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FidelitasHub.Models
{
    public class ProductivityGroupAssignment
    {
        [Key]
        public int ProductivityGroupAssignmentId { get; set; }

        public int ProductivityGroupId { get; set; }
        [ForeignKey(nameof(ProductivityGroupId))]
        public ProductivityGroup? Group { get; set; }

        // NULL means ALL CLIENTS. A value means one specific client.
        public int? ClientId { get; set; }
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
        public string? CreatedBy { get; set; }
    }
}
