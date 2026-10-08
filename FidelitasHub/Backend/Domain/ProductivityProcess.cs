using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FidelitasHub.Models
{
    public class ProductivityProcess
    {
        [Key]
        public int ProductivityProcessId { get; set; }

        [Required, MaxLength(30)]
        public string ProcessCode { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string ProcessName { get; set; } = string.Empty;

        public int? ClientId { get; set; }

        [ForeignKey(nameof(ClientId))]
        public Client? Client { get; set; }

        public int? DepartmentId { get; set; }

        [ForeignKey(nameof(DepartmentId))]
        public Department? Department { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public DateTime? ModifiedOn { get; set; }

        public ICollection<ProductivityActivity> Activities { get; set; } = new List<ProductivityActivity>();
    }
}
