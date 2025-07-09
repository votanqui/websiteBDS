using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace thuctap2025.Models
{
    public class Users
    {
        public int Id { get; set; }
        public string UserName { get; set; } = null!;

        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PasswordHash { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public string AccountStatus { get; set; } = "Pending";
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLogin { get; set; }
        public string? EmailConfirmationToken { get; set; }
        public DateTime? EmailConfirmationTokenExpiry { get; set; }
        public bool IsEmailConfirmed { get; set; } = false;
        public decimal Amount { get; set; } = 0;

        public ICollection<Property> Properties { get; set; }
        public ICollection<Favorite> Favorites { get; set; }
    }

}
