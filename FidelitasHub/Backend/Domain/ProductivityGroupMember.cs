using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FidelitasHub.Models
{
    public class ProductivityGroupMember
    {
        [Key]
        public int ProductivityGroupMemberId { get; set; }

        public int ProductivityGroupId { get; set; }
        [ForeignKey(nameof(ProductivityGroupId))]
        public ProductivityGroup? Group { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        [Required]
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }
}
