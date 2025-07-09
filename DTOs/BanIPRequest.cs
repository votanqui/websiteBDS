namespace thuctap2025.DTOs
{
    public class BanIPRequest
    {
        public string IPAddress { get; set; } = null!;
        public string? BanReason { get; set; }
    }
}
