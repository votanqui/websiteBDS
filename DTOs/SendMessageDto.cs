namespace thuctap2025.DTOs
{
    public class SendMessageDto
    {
        public string ReceiverId { get; set; }        // ID người nhận
        public string Content { get; set; }           // Nội dung tin nhắn (có thể rỗng nếu là ảnh)
        public string MessageType { get; set; }       // "text", "image", v.v.
        public string ImageUrl { get; set; }          // Đường dẫn ảnh nếu là tin nhắn ảnh
    }

}
