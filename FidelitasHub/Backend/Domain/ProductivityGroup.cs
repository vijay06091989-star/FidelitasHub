using System.ComponentModel.DataAnnotations;

namespace FidelitasHub.Models
{
    public class ProductivityGroup
    {
        [Key]
        public int ProductivityGroupId { get; set; }

        [Required, MaxLength(100)]
        public string GroupName { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public DateTime? ModifiedOn { get; set; }
        public string? CreatedBy { get; set; }

        public ICollection<ProductivityGroupMember> Members { get; set; } = new List<ProductivityGroupMember>();
        public ICollection<ProductivityGroupAssignment> Assignments { get; set; } = new List<ProductivityGroupAssignment>();
    }
}
