namespace thuctap2025.Models
{
    public class ContactPageSettings
    {
        public int Id { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string MapEmbedUrl { get; set; }
        public string OpeningHours { get; set; }
        public string Facebook { get; set; }
        public string Zalo { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

}
