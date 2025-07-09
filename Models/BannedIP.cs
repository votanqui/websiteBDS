namespace thuctap2025.Models
{
    public class BannedIP
    {
        public int Id { get; set; }

        public string IPAddress { get; set; } = null!;

        public string? BanReason { get; set; }

        public DateTime BannedAt { get; set; } = DateTime.Now;

        public string? BannedBy { get; set; } // ai ban (admin username hoặc system)
    }
}
