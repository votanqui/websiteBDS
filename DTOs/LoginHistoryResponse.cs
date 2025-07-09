namespace thuctap2025.DTOs
{
    public class LoginHistoryResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string? FullName { get; set; }
        public string IPAddress { get; set; } = null!;
        public DateTime LoginTime { get; set; }
        public string? DeviceInfo { get; set; }
    }
}
