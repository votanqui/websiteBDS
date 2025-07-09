namespace thuctap2025.DTOs
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public DateTime JoinDate { get; set; }
        public int TotalProperties { get; set; }
        public int ProfileViewCount { get; set; }
    }
}
