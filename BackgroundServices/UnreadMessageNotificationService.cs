using Microsoft.EntityFrameworkCore;
using thuctap2025.Data;
using thuctap2025.DTOs;
using thuctap2025.Models;
using thuctap2025.Services;

namespace thuctap2025.BackgroundServices
{
    public class UnreadMessageNotificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UnreadMessageNotificationService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(4); // 4 giờ

        public UnreadMessageNotificationService(
            IServiceProvider serviceProvider,
            ILogger<UnreadMessageNotificationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessUnreadMessagesAsync();
                    _logger.LogInformation($"Unread message notification check completed at {DateTime.Now}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing unread messages notification");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task ProcessUnreadMessagesAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            // Lấy danh sách users có tin nhắn chưa đọc kèm theo thông tin người gửi
            var usersWithUnreadMessages = await dbContext.ChatMessage
                .Where(m => !m.IsRead)
                .Join(dbContext.Users,
                    message => message.SenderId,
                    sender => sender.Id.ToString(),
                    (message, sender) => new { Message = message, Sender = sender })
                .GroupBy(x => x.Message.ReceiverId)
                .Select(g => new
                {
                    ReceiverId = g.Key,
                    UnreadCount = g.Count(),
                    Messages = g.OrderByDescending(x => x.Message.SentAt)
                               .Select(x => new ChatMessageWithSender
                               {
                                   Id = x.Message.Id,
                                   SenderId = x.Message.SenderId,
                                   SenderFullName = x.Sender.FullName ?? x.Sender.UserName ?? "Người dùng ẩn danh",
                                   Content = x.Message.Content,
                                   SentAt = x.Message.SentAt,
                                   IsRead = x.Message.IsRead
                               }).ToList()
                })
                .ToListAsync();

            foreach (var userMessages in usersWithUnreadMessages)
            {
                try
                {
                    // Lấy thông tin user nhận tin nhắn
                    var user = await dbContext.Users
                        .FirstOrDefaultAsync(u => u.Id.ToString() == userMessages.ReceiverId);

                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        // Kiểm tra thời gian login cuối cùng (chỉ gửi email nếu user không online gần đây)
                        var shouldSendEmail = user.LastLogin == null ||
                                            user.LastLogin < DateTime.Now.AddHours(-1);

                        if (shouldSendEmail)
                        {
                            await emailService.SendUnreadMessagesNotificationAsync(
                                user.Email,
                                user.FullName ?? user.UserName ?? "Người dùng",
                                userMessages.Messages);

                            _logger.LogInformation($"Notification email sent to user {user.UserName} for {userMessages.UnreadCount} unread messages");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send notification email to user {userMessages.ReceiverId}");
                }
            }
        }
    }
}