using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using thuctap2025.Data;
using thuctap2025.Models;

[Authorize]
public class ChatHub : Hub
{
    private readonly ApplicationDbContext _context;

    public ChatHub(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SendMessage(string receiverId, string content)
    {
        var senderId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(receiverId))
        {
            throw new HubException("Invalid sender or receiver");
        }

        var message = new ChatMessage
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.ChatMessage.Add(message);
        await _context.SaveChangesAsync();

        // Gửi tin nhắn tới người nhận (nếu đang online)
        await Clients.User(receiverId).SendAsync("ReceiveMessage", new
        {
            Id = message.Id,
            SenderId = senderId,
            Content = content,
            SentAt = message.SentAt
        });

        // Gửi lại cho người gửi để cập nhật UI
        await Clients.User(receiverId).SendAsync("ReceiveMessage", new
        {
            Id = message.Id,
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            SentAt = message.SentAt
        });

        await Clients.Caller.SendAsync("MessageSent", new
        {
            Id = message.Id,
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            SentAt = message.SentAt
        });

    }
    public async Task SendImage(string receiverId, string imageUrl)
    {
        var senderId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(receiverId))
        {
            throw new HubException("Invalid sender or receiver");
        }

        var message = new ChatMessage
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            ImageURL = imageUrl, // Lưu URL ảnh
            Content = "null",      // Tin nhắn ảnh sẽ không có content
            SentAt = DateTime.Now,
            IsRead = false
        };

        _context.ChatMessage.Add(message);
        await _context.SaveChangesAsync();

        // Gửi cho người nhận
        await Clients.User(receiverId).SendAsync("ReceiveMessage", new
        {
            Id = message.Id,
            SenderId = senderId,
            ImageURL = message.ImageURL,
            Content = message.Content,
            SentAt = message.SentAt
        });

        // Gửi lại cho người gửi
        await Clients.Caller.SendAsync("MessageSent", new
        {
            Id = message.Id,
            SenderId = senderId,
            ReceiverId = receiverId,
            ImageURL = message.ImageURL,
            Content = message.Content,
            SentAt = message.SentAt
        });
    }

    public async Task DeleteMessage(int messageId)
    {
        var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var message = await _context.ChatMessage.FindAsync(messageId);

        if (message == null)
        {
            throw new HubException("Message not found");
        }

        if (message.SenderId != userId && message.ReceiverId != userId)
        {
            throw new HubException("Unauthorized to delete this message");
        }
        var timeSinceSent = DateTime.UtcNow - message.SentAt;
        if (timeSinceSent > TimeSpan.FromMinutes(15))
        {
            throw new HubException("Messages can only be deleted within 15 minutes of sending");
        }
        _context.ChatMessage.Remove(message);
        await _context.SaveChangesAsync();

        // Notify both parties about the deletion
        await Clients.Users(message.SenderId, message.ReceiverId)
            .SendAsync("MessageDeleted", message.Id);
    }
    public async Task MarkAsRead(int messageId)
    {
        var message = await _context.ChatMessage.FindAsync(messageId);
        if (message != null && message.ReceiverId == Context.User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            message.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        await base.OnDisconnectedAsync(exception);
    }
    public async Task MarkAllAsRead(string senderId)
    {
        var receiverId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        var unreadMessages = await _context.ChatMessage
            .Where(m => m.SenderId == senderId &&
                       m.ReceiverId == receiverId &&
                       !m.IsRead)
            .ToListAsync();

        foreach (var message in unreadMessages)
        {
            message.IsRead = true;
        }

        await _context.SaveChangesAsync();

        // Gửi update cho cả hai bên
        await Clients.User(senderId).SendAsync("MessagesRead", receiverId);
        await Clients.User(receiverId).SendAsync("UnreadCountUpdated", new
        {
            senderId,
            count = 0
        });

        // Gửi update tổng số unread mới
        var newUnreadCount = await GetUnreadCountForUser(receiverId);
        await Clients.User(receiverId).SendAsync("TotalUnreadUpdated", newUnreadCount);
    }

    private async Task<Dictionary<string, int>> GetUnreadCountForUser(string userId)
    {
        var cutoffDate = DateTime.UtcNow.AddMonths(-1);

        return await _context.ChatMessage
            .Where(m => m.ReceiverId == userId &&
                       !m.IsRead &&
                       m.SentAt >= cutoffDate)
            .GroupBy(m => m.SenderId)
            .ToDictionaryAsync(
                g => g.Key,
                g => g.Count()
            );
    }
}