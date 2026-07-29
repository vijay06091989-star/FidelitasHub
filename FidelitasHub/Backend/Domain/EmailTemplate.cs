using System.ComponentModel.DataAnnotations;

namespace FidelitasHub.Models
{
    public class EmailTemplate
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string TemplateCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string TemplateName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = "General";

        [Required]
        [MaxLength(250)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public DateTime? ModifiedOn { get; set; }
    }
}