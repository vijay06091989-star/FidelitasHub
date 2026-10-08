using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FidelitasHub.Models
{
    public class ProductivityEntry
    {
        [Key]
        public int ProductivityEntryId { get; set; }

        [Required]
        public DateTime ProductionDate { get; set; }

        public int EmployeeId { get; set; }
        [ForeignKey(nameof(EmployeeId))]
        public Employee? Employee { get; set; }

        public int ClientId { get; set; }
        [ForeignKey(nameof(ClientId))]
        public Client? Client { get; set; }

        public int ProductivityActivityId { get; set; }
        [ForeignKey(nameof(ProductivityActivityId))]
        public ProductivityActivity? Activity { get; set; }

        public decimal Quantity { get; set; }

        // Snapshots deliberately preserve historical calculations when setup changes later.
        public decimal TargetPerDaySnapshot { get; set; }
        public decimal WeightSnapshot { get; set; }
        public decimal WeightedAchievement { get; set; }

        [MaxLength(1000)]
        public string? Remarks { get; set; }

        public int EnteredByEmployeeId { get; set; }
        [ForeignKey(nameof(EnteredByEmployeeId))]
        public Employee? EnteredByEmployee { get; set; }
        public DateTime EnteredOn { get; set; } = DateTime.Now;

        [MaxLength(30)]
        public string Status { get; set; } = "Final";
    }
}
