using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FidelitasHub.Models
{
    public class ClientWebLogin
    {
        [Key]
        public int ClientWebLoginId { get; set; }

        [Required]
        public int ClientId { get; set; }

        [ForeignKey(nameof(ClientId))]
        public Client? Client { get; set; }

        [Required]
        [MaxLength(250)]
        public string Website { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Url { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string EncryptedPassword { get; set; } = string.Empty;

        public string? EncryptedSecurityQuestions { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [MaxLength(200)]
        public string? CreatedBy { get; set; }

        public DateTime? ModifiedOn { get; set; }

        [MaxLength(200)]
        public string? ModifiedBy { get; set; }
    }
}
