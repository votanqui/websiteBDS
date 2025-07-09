namespace thuctap2025.DTOs
{
    public class BannedIPResponse
    {
        public int Id { get; set; }
        public string IPAddress { get; set; } = null!;
        public string? BanReason { get; set; }
        public DateTime BannedAt { get; set; }
        public string? BannedBy { get; set; }
    }
}
