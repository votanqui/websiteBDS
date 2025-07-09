﻿namespace thuctap2025.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }
        public string Content { get; set; }
        public string? ImageURL { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
     
    }
}
