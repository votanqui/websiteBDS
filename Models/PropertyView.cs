namespace thuctap2025.Models
{
    public class PropertyView
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public int? UserId { get; set; }
        public DateTime ViewedAt { get; set; } = DateTime.Now;
        public string IPAddress { get; set; }
        public string UserAgent { get; set; }

        public Property Property { get; set; }
        public Users User { get; set; }
    }
}
