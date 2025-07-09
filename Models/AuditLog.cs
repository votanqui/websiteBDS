namespace thuctap2025.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string ActionType { get; set; } = null!; // Create, Update, Delete, Login, etc.
        public string TableName { get; set; } = null!;
        public int? RecordId { get; set; }  // Id của bản ghi bị ảnh hưởng
        public int? UserId { get; set; }  // Ai thực hiện hành động
        public string? IPAddress { get; set; }
        public string? UserAgent { get; set; }
        public DateTime ActionTime { get; set; } = DateTime.Now;
        public string? OldValues { get; set; } // JSON string
        public string? NewValues { get; set; } // JSON string
    }

}
