namespace thuctap2025.Models
{
    public class UserProfileView
    {
        public int Id { get; set; }

        public int ViewedUserId { get; set; }  // Hồ sơ của ai
        public Users ViewedUser { get; set; }

        public int? ViewerUserId { get; set; }  // Ai xem (null nếu anonymous)
        public Users ViewerUser { get; set; }

        public DateTime ViewedAt { get; set; } = DateTime.Now;

        public string IPAddress { get; set; }
        public string UserAgent { get; set; }
    }
}
