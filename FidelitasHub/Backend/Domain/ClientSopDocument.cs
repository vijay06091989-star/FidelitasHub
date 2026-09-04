using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FidelitasHub.Models
{
    public class ClientSopDocument
    {
        [Key]
        public int ClientSopDocumentId { get; set; }

        [Required]
        public int ClientId { get; set; }

        [ForeignKey(nameof(ClientId))]
        public Client? Client { get; set; }

        [Required]
        public int Version { get; set; }

        [Required]
        [MaxLength(500)]
        public string DisplayFileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string StoredFilePath { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ContentType { get; set; }

        public long FileSizeBytes { get; set; }

        [Required]
        [MaxLength(200)]
        public string UploadedBy { get; set; } = string.Empty;

        public DateTime UploadedOn { get; set; } = DateTime.Now;

        public bool IsCurrent { get; set; } = true;
    }
}
