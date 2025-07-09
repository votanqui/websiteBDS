namespace thuctap2025.Models
{
    public class UserLoginHistory
    {
        public int Id { get; set; }

        public int UserId { get; set; } // khóa ngoại về Users

        public string IPAddress { get; set; } = null!;

        public DateTime LoginTime { get; set; } = DateTime.Now;

        public string? DeviceInfo { get; set; } // tuỳ chọn: thông tin thiết bị

        // Navigation property
        public Users User { get; set; } = null!;
    }
}
