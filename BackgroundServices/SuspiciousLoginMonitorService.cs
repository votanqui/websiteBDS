using Microsoft.EntityFrameworkCore;
using thuctap2025.Data;
using thuctap2025.Models;
using thuctap2025.Services;

namespace thuctap2025.BackgroundServices
{
    public class SuspiciousLoginMonitorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SuspiciousLoginMonitorService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(10); // Kiểm tra 10 phút một lần
        private const int MAX_ACCOUNTS_PER_IP = 10; // Tối đa 10 tài khoản per IP
        private const int TIME_WINDOW_HOURS = 1; // Trong vòng 1 giờ

        public SuspiciousLoginMonitorService(
            IServiceProvider serviceProvider,
            ILogger<SuspiciousLoginMonitorService> logger)
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
                    await MonitorSuspiciousLoginsAsync();
                    _logger.LogInformation($"Suspicious login check completed at {DateTime.Now}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while monitoring suspicious logins");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task MonitorSuspiciousLoginsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var now = DateTime.Now;
            var timeThreshold = now.AddHours(-TIME_WINDOW_HOURS);

            // Lấy các IP đã bị ban để không xử lý nữa
            var bannedIPs = await dbContext.BannedIPs
                .Select(b => b.IPAddress)
                .ToListAsync();

            // Tìm các IP có nhiều login khác nhau trong thời gian gần đây
            var suspiciousIPs = await dbContext.UserLoginHistories
                .Where(h => h.LoginTime >= timeThreshold && !bannedIPs.Contains(h.IPAddress))
                .GroupBy(h => h.IPAddress)
                .Select(g => new
                {
                    IPAddress = g.Key,
                    UniqueUserCount = g.Select(x => x.UserId).Distinct().Count(),
                    LoginCount = g.Count(),
                    FirstLogin = g.Min(x => x.LoginTime),
                    LastLogin = g.Max(x => x.LoginTime),
                    UserIds = g.Select(x => x.UserId).Distinct().ToList()
                })
                .Where(x => x.UniqueUserCount >= MAX_ACCOUNTS_PER_IP)
                .ToListAsync();

            foreach (var suspiciousIP in suspiciousIPs)
            {
                try
                {
                    await ProcessSuspiciousIPAsync(dbContext, emailService, suspiciousIP.IPAddress,
                        suspiciousIP.UniqueUserCount, suspiciousIP.LoginCount,
                        suspiciousIP.FirstLogin, suspiciousIP.LastLogin, suspiciousIP.UserIds);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to process suspicious IP {suspiciousIP.IPAddress}");
                }
            }

            await dbContext.SaveChangesAsync();
        }

        private async Task ProcessSuspiciousIPAsync(
            ApplicationDbContext dbContext,
            IEmailService emailService,
            string ipAddress,
            int uniqueUserCount,
            int totalLoginCount,
            DateTime firstLogin,
            DateTime lastLogin,
            List<int> affectedUserIds)
        {
            // Kiểm tra xem IP đã bị ban chưa
            var existingBan = await dbContext.BannedIPs
                .FirstOrDefaultAsync(b => b.IPAddress == ipAddress);

            if (existingBan != null)
            {
                _logger.LogInformation($"IP {ipAddress} is already banned, skipping");
                return;
            }

            // Tạo ban record
            var bannedIP = new BannedIP
            {
                IPAddress = ipAddress,
                BanReason = $"Tự động ban do đăng nhập {uniqueUserCount} tài khoản khác nhau trong {TIME_WINDOW_HOURS} giờ " +
                           $"(Tổng {totalLoginCount} lần đăng nhập từ {firstLogin:HH:mm dd/MM/yyyy} đến {lastLogin:HH:mm dd/MM/yyyy})",
                BannedAt = DateTime.Now,
                BannedBy = "SYSTEM_AUTO_BAN"
            };

            dbContext.BannedIPs.Add(bannedIP);

            // Log chi tiết
            _logger.LogWarning($"SUSPICIOUS ACTIVITY DETECTED - Auto-banning IP {ipAddress}:");
            _logger.LogWarning($"- Unique users logged in: {uniqueUserCount}");
            _logger.LogWarning($"- Total login attempts: {totalLoginCount}");
            _logger.LogWarning($"- Time range: {firstLogin:HH:mm dd/MM/yyyy} - {lastLogin:HH:mm dd/MM/yyyy}");
            _logger.LogWarning($"- Affected user IDs: {string.Join(", ", affectedUserIds)}");

            // Lấy thông tin users bị ảnh hưởng để gửi email cảnh báo
            var affectedUsers = await dbContext.Users
                .Where(u => affectedUserIds.Contains(u.Id) && !string.IsNullOrEmpty(u.Email))
                .ToListAsync();

            // Gửi email cảnh báo cho các users bị ảnh hưởng
            foreach (var user in affectedUsers)
            {
                try
                {
                    // Đây là một method mới cần thêm vào IEmailService
                    await emailService.SendSuspiciousActivityNotificationAsync(
                        user.Email!,
                        user.FullName ?? user.UserName,
                        ipAddress,
                        lastLogin);

                    _logger.LogInformation($"Suspicious activity notification sent to user {user.UserName}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send suspicious activity notification to user {user.UserName}");
                }
            }

            // Gửi email báo cáo cho admin (optional)
            await SendAdminSuspiciousActivityReportAsync(emailService, ipAddress, uniqueUserCount,
                totalLoginCount, firstLogin, lastLogin, affectedUsers);

            _logger.LogInformation($"Successfully banned suspicious IP {ipAddress}");
        }

        private async Task SendAdminSuspiciousActivityReportAsync(
            IEmailService emailService,
            string ipAddress,
            int uniqueUserCount,
            int totalLoginCount,
            DateTime firstLogin,
            DateTime lastLogin,
            List<Users> affectedUsers)
        {
            try
            {
                // Email admin - cần config email admin trong appsettings
                var adminEmail = "votanqui29052003@gmail.com"; // Có thể lấy từ configuration

                // Tạo báo cáo chi tiết
                var report = new
                {
                    IPAddress = ipAddress,
                    UniqueUserCount = uniqueUserCount,
                    TotalLoginCount = totalLoginCount,
                    FirstLogin = firstLogin,
                    LastLogin = lastLogin,
                    AffectedUsers = affectedUsers.Select(u => new
                    {
                        u.Id,
                        u.UserName,
                        u.Email,
                        u.FullName
                    }).ToList()
                };

                // Method này cũng cần thêm vào IEmailService
                await emailService.SendAdminSuspiciousActivityReportAsync(adminEmail, report);

                _logger.LogInformation($"Admin report sent for suspicious IP {ipAddress}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send admin report for suspicious IP {ipAddress}");
            }
        }
    }
}