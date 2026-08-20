using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FidelitasHub.Models
{
    public class Client
    {
        [Key]
        public int ClientId { get; set; }

        [Required]
        [MaxLength(30)]
        public string ClientCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string ClientName { get; set; } = string.Empty;


        // ==================================================
        // GENERAL SHIFT MANAGER
        // ==================================================

        public int? GeneralShiftManagerId { get; set; }

        [ForeignKey(nameof(GeneralShiftManagerId))]
        public Employee? GeneralShiftManager { get; set; }


        // ==================================================
        // GENERAL SHIFT TEAM LEADERS
        // ==================================================

        // Billing Team Leader
        public int? GeneralShiftBillingTeamLeaderId { get; set; }

        [ForeignKey(nameof(GeneralShiftBillingTeamLeaderId))]
        public Employee? GeneralShiftBillingTeamLeader { get; set; }


        // Posting Team Leader
        public int? GeneralShiftPostingTeamLeaderId { get; set; }

        [ForeignKey(nameof(GeneralShiftPostingTeamLeaderId))]
        public Employee? GeneralShiftPostingTeamLeader { get; set; }


        // DM Team Leader
        public int? GeneralShiftDMTeamLeaderId { get; set; }

        [ForeignKey(nameof(GeneralShiftDMTeamLeaderId))]
        public Employee? GeneralShiftDMTeamLeader { get; set; }


        // End to End Team Leader
        public int? GeneralShiftEndToEndTeamLeaderId { get; set; }

        [ForeignKey(nameof(GeneralShiftEndToEndTeamLeaderId))]
        public Employee? GeneralShiftEndToEndTeamLeader { get; set; }


        // ==================================================
        // US SHIFT
        // ==================================================

        public int? USShiftManagerId { get; set; }

        [ForeignKey(nameof(USShiftManagerId))]
        public Employee? USShiftManager { get; set; }


        public int? USShiftTeamLeaderId { get; set; }

        [ForeignKey(nameof(USShiftTeamLeaderId))]
        public Employee? USShiftTeamLeader { get; set; }


        // ==================================================
        // STATUS
        // ==================================================

        public bool IsActive { get; set; } = true;


        // ==================================================
        // AUDIT
        // ==================================================

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public string? CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        public string? ModifiedBy { get; set; }
    }
}