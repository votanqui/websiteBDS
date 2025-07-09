using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using thuctap2025.Data;
using thuctap2025.DTOs;
using thuctap2025.Models;


[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ChatController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var conversations = await _context.ChatMessage
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Select(g => new
            {
                UserId = g.Key,
                User = _context.Users.FirstOrDefault(u => u.Id.ToString() == g.Key),
                LastMessage = g.OrderByDescending(m => m.SentAt).FirstOrDefault()
            })
            .ToListAsync();

        return Ok(conversations.Select(c => new {
            UserId = c.UserId,
            FullName = c.User != null ? c.User.FullName : "Unknown User",
            LastMessageContent = c.LastMessage?.Content,
            LastMessageTime = c.LastMessage?.SentAt
        }));
    }
    [HttpGet("messages/{otherUserId}")]
    public async Task<IActionResult> GetMessages(string otherUserId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var messages = await _context.ChatMessage
            .Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                       (m.SenderId == otherUserId && m.ReceiverId == userId))
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        return Ok(messages);
    }
    [HttpPost("UploadImages")]
    [Authorize]
    public async Task<IActionResult> UploadImages([FromForm] List<IFormFile> files)

    {
        if (files == null || files.Count == 0)
            return BadRequest("No files uploaded");

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "chat-images");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var uploadedUrls = new List<string>();

        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            // Generate unique filename
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            uploadedUrls.Add($"/chat-images/{uniqueFileName}");
        }

        return Ok(uploadedUrls);
    }
    [Authorize]
    [HttpDelete("RemoveChatImage")]
    public async Task<IActionResult> RemoveChatImage([FromQuery] string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return BadRequest("Tên ảnh không được để trống.");

        // Gắn lại /images/ nếu chưa có
   

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;


        var chatMessage = await _context.ChatMessage
            .FirstOrDefaultAsync(cm => cm.ImageURL == imageUrl);

        if (chatMessage == null)
            return NotFound("Không tìm thấy ảnh trong tin nhắn.");
        var timeSinceSent = DateTime.Now - chatMessage.SentAt;
        if (timeSinceSent > TimeSpan.FromMinutes(15))
        {
            return BadRequest(new { message = "Images can only be deleted within 15 minutes of sending" });
        }
        if (chatMessage.SenderId != userId && userRole != "Admin")
            return StatusCode(403, new { message = "Bạn không có quyền xóa ảnh này." });

        var fileNameOnly = imageUrl.Replace("/chat-images/", "").TrimStart('/');
        var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/chat-images", fileNameOnly);

        if (System.IO.File.Exists(imagePath))
            System.IO.File.Delete(imagePath);

        chatMessage.ImageURL = null;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Xóa ảnh thành công." });
    }

    [HttpPost("send")]
    [Authorize]
    public async Task<IActionResult> SendMessage([FromBody] ChatSendRequest request)
    {
        var senderIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!int.TryParse(senderIdString, out var senderId))
        {
            return BadRequest(new
            {
                success = false,
                message = "Không xác định được người gửi."
            });
        }

        if (request.ReceiverId == 0 || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new
            {
                success = false,
                message = "Thiếu thông tin người nhận hoặc nội dung."
            });
        }

        if (request.ReceiverId == senderId)
        {
            return BadRequest(new
            {
                success = false,
                message = "Không thể gửi tin nhắn cho chính mình."
            });
        }

        var message = new ChatMessage
        {
            SenderId = senderId.ToString(),
            ReceiverId = request.ReceiverId.ToString(),
            Content = request.Content,
            SentAt = DateTime.Now,
            IsRead = false
        };

        _context.ChatMessage.Add(message);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                message.Id,
                message.SenderId,
                message.ReceiverId,
                message.Content,
                message.SentAt
            }
        });
    }
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var cutoffDate = DateTime.Now.AddMonths(-1); // Chỉ lấy tin nhắn trong 1 tháng gần đây

        var unreadCount = await _context.ChatMessage
            .Where(m => m.ReceiverId == userId && !m.IsRead && m.SentAt >= cutoffDate)
            .GroupBy(m => m.SenderId)
            .Select(g => new {
                SenderId = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        return Ok(unreadCount);
    }
}