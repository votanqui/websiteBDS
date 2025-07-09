using Microsoft.EntityFrameworkCore;
using thuctap2025.Data;
using thuctap2025.DTOs;
using thuctap2025.Models;
using thuctap2025.Services;

namespace thuctap2025.BackgroundServices
{
    public class VipNotificationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<VipNotificationService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(6); // Kiểm tra 6 giờ một lần

        public VipNotificationService(
            IServiceProvider serviceProvider,
            ILogger<VipNotificationService> logger)
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
                    await ProcessVipNotificationsAsync();
                    _logger.LogInformation($"VIP notification check completed at {DateTime.Now}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing VIP notifications");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task ProcessVipNotificationsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var now = DateTime.Now;

            // 1. Tìm các property VIP đã hết hạn và cập nhật trạng thái
            await ProcessExpiredVipPropertiesAsync(dbContext, emailService, now);

            // 2. Gửi thông báo sắp hết hạn VIP (3 ngày trước khi hết hạn)
            await ProcessVipExpiringNotificationsAsync(dbContext, emailService, now);

            await dbContext.SaveChangesAsync();
        }

        private async Task ProcessExpiredVipPropertiesAsync(ApplicationDbContext dbContext, IEmailService emailService, DateTime now)
        {
            // Lấy các property VIP đã hết hạn
            var expiredVipProperties = await dbContext.Properties
                .Include(p => p.User)
                .Where(p => p.IsVip && p.VipEndDate.HasValue && p.VipEndDate <= now)
                .ToListAsync();

            foreach (var property in expiredVipProperties)
            {
                try
                {
                    // Cập nhật trạng thái VIP
                    property.IsVip = false;
                    property.VipStartDate = null;
                    property.VipEndDate = null;

                    // Gửi email thông báo hết hạn VIP
                    if (property.User != null && !string.IsNullOrEmpty(property.User.Email))
                    {
                        await emailService.SendVipExpiredNotificationAsync(
                            property.User.Email,
                            property.User.FullName ?? property.User.UserName ?? "Người dùng",
                            property);

                        _logger.LogInformation($"VIP expired notification sent for property {property.Id} to user {property.User.UserName}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to process expired VIP property {property.Id}");
                }
            }
        }

        private async Task ProcessVipExpiringNotificationsAsync(ApplicationDbContext dbContext, IEmailService emailService, DateTime now)
        {
            var threeDaysFromNow = now.AddDays(3);
            var oneDayFromNow = now.AddDays(1);

            // Lấy các property VIP sắp hết hạn trong 3 ngày
            var expiringVipProperties = await dbContext.Properties
                .Include(p => p.User)
                .Where(p => p.IsVip &&
                           p.VipEndDate.HasValue &&
                           p.VipEndDate <= threeDaysFromNow &&
                           p.VipEndDate > now)
                .ToListAsync();

            foreach (var property in expiringVipProperties)
            {
                try
                {
                    if (property.User != null && !string.IsNullOrEmpty(property.User.Email))
                    {
                        var daysLeft = (property.VipEndDate.Value - now).Days;

                        // Chỉ gửi thông báo vào các mốc: 3 ngày, 1 ngày, và ngày cuối
                        if (daysLeft == 3 || daysLeft == 1 ||
                            (property.VipEndDate.Value.Date == now.Date && property.VipEndDate.Value.Hour <= now.Hour + 6))
                        {
                            await emailService.SendVipExpiringNotificationAsync(
                                property.User.Email,
                                property.User.FullName ?? property.User.UserName ?? "Người dùng",
                                property,
                                daysLeft);

                            _logger.LogInformation($"VIP expiring notification sent for property {property.Id} to user {property.User.UserName}, {daysLeft} days left");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send VIP expiring notification for property {property.Id}");
                }
            }
        }
    }
}