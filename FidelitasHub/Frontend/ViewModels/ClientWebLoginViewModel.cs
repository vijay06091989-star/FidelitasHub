using System.ComponentModel.DataAnnotations;

namespace FidelitasHub.Models
{
    public class ClientWebLoginViewModel
    {
        public int ClientWebLoginId { get; set; }

        [Required]
        public int ClientId { get; set; }

        public string ClientName { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        [Display(Name = "Website / Portal")]
        public string Website { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        [Display(Name = "URL Link")]
        public string Url { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        [Display(Name = "User Name")]
        public string Username { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [Display(Name = "Security Questions")]
        public string? SecurityQuestions { get; set; }

        public bool ClearSecurityQuestions { get; set; }
    }

    public class ClientWebLoginDisplayViewModel
    {
        public int ClientWebLoginId { get; set; }

        public int ClientId { get; set; }

        public string Website { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string SecurityQuestions { get; set; } = string.Empty;
    }
}
