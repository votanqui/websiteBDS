using System.ComponentModel.DataAnnotations;

namespace thuctap2025.DTOs
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
    }
}
