namespace thuctap2025.DTOs
{
    public class ChatMessageWithSender
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = null!;
        public string SenderFullName { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
    }
}
