namespace thuctap2025.Models
{
    public class NewsView
    {
        public int Id { get; set; }

        public int NewsId { get; set; }
        public News News { get; set; }

        public int? UserId { get; set; }   // Có thể null nếu anonymous
        public Users User { get; set; }

        public DateTime ViewedAt { get; set; } = DateTime.Now;

        public string IPAddress { get; set; }
        public string UserAgent { get; set; }
    }
}
