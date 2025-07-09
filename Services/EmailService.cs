using System.Net.Mail;
using System.Net;
using System.Text;
using thuctap2025.Models;
using Microsoft.Extensions.Configuration;
using thuctap2025.DTOs;

namespace thuctap2025.Services
{
    public class EmailService : IEmailService
    {
        private const string Email = "nhom8provip@gmail.com";
        private const string Password = "f y w x tr r m h w v e m q h u";
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;

        public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task SendUnreadMessagesNotificationAsync(string userEmail, string userName, List<ChatMessageWithSender> unreadMessages)
        {
            try
            {
                using var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(Email, Password)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(Email, "Real Estate Platform"),
                    Subject = "Bạn có tin nhắn chưa đọc",
                    Body = GenerateEmailBody(userName, unreadMessages),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(userEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email sent successfully to {userEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {userEmail}");
            }
        }

        private string GenerateEmailBody(string userName, List<ChatMessageWithSender> unreadMessages)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'>");
            sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.AppendLine("<title>Thông báo tin nhắn mới</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("* { margin: 0; padding: 0; box-sizing: border-box; }");
            sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; background-color: #f8f9fa; }");
            sb.AppendLine(".email-wrapper { background-color: #f8f9fa; padding: 20px 0; min-height: 100vh; }");
            sb.AppendLine(".container { max-width: 650px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 8px 32px rgba(0,0,0,0.1); }");

            // Header với gradient đẹp
            sb.AppendLine(".header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px 30px; text-align: center; position: relative; }");
            sb.AppendLine(".header::before { content: ''; position: absolute; top: 0; left: 0; right: 0; bottom: 0; background: url('data:image/svg+xml,<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 100 100\"><defs><pattern id=\"grain\" width=\"100\" height=\"100\" patternUnits=\"userSpaceOnUse\"><circle cx=\"25\" cy=\"25\" r=\"1\" fill=\"%23ffffff\" opacity=\"0.1\"/><circle cx=\"75\" cy=\"75\" r=\"1\" fill=\"%23ffffff\" opacity=\"0.05\"/><circle cx=\"50\" cy=\"10\" r=\"0.5\" fill=\"%23ffffff\" opacity=\"0.1\"/></pattern></defs><rect width=\"100\" height=\"100\" fill=\"url(%23grain)\"/></svg>') repeat; }");
            sb.AppendLine(".header h1 { font-size: 28px; font-weight: 600; margin-bottom: 8px; position: relative; z-index: 1; }");
            sb.AppendLine(".header .subtitle { font-size: 16px; opacity: 0.9; position: relative; z-index: 1; }");
            sb.AppendLine(".notification-badge { display: inline-block; background-color: #ff4757; color: white; padding: 6px 12px; border-radius: 20px; font-size: 14px; font-weight: 600; margin-left: 8px; }");

            // Content area
            sb.AppendLine(".content { padding: 40px 30px; }");
            sb.AppendLine(".greeting { font-size: 18px; color: #2d3436; margin-bottom: 25px; }");
            sb.AppendLine(".greeting .username { color: #6c5ce7; font-weight: 600; }");
            sb.AppendLine(".summary { background: linear-gradient(135deg, #74b9ff, #0984e3); color: white; padding: 20px; border-radius: 10px; margin-bottom: 30px; text-align: center; }");
            sb.AppendLine(".summary h3 { font-size: 20px; margin-bottom: 5px; }");
            sb.AppendLine(".summary p { opacity: 0.9; }");

            // Message items với design cải tiến
            sb.AppendLine(".messages-container { margin-bottom: 30px; }");
            sb.AppendLine(".message-item { background: #ffffff; margin: 15px 0; padding: 20px; border-radius: 10px; border: 1px solid #e9ecef; transition: all 0.3s ease; position: relative; }");
            sb.AppendLine(".message-item::before { content: ''; position: absolute; left: 0; top: 0; bottom: 0; width: 4px; background: linear-gradient(135deg, #667eea, #764ba2); border-radius: 2px; }");
            sb.AppendLine(".message-item:hover { box-shadow: 0 4px 20px rgba(0,0,0,0.08); transform: translateY(-2px); }");
            sb.AppendLine(".sender-info { display: flex; align-items: center; margin-bottom: 12px; }");
            sb.AppendLine(".sender-avatar { width: 40px; height: 40px; border-radius: 50%; background: linear-gradient(135deg, #667eea, #764ba2); display: flex; align-items: center; justify-content: center; color: white; font-weight: 600; font-size: 16px; margin-right: 12px; }");
            sb.AppendLine(".sender-details h4 { color: #2d3436; font-size: 16px; font-weight: 600; margin-bottom: 2px; }");
            sb.AppendLine(".sender-details .time { color: #636e72; font-size: 13px; }");
            sb.AppendLine(".message-content { color: #2d3436; font-size: 15px; line-height: 1.6; padding: 15px; background-color: #f8f9fa; border-radius: 8px; border-left: 3px solid #667eea; }");

            // More messages indicator
            sb.AppendLine(".more-messages { text-align: center; padding: 20px; background: linear-gradient(135deg, #fd79a8, #e84393); color: white; border-radius: 10px; margin: 20px 0; }");
            sb.AppendLine(".more-messages .count { font-size: 24px; font-weight: 700; display: block; }");
            sb.AppendLine(".more-messages .text { font-size: 14px; opacity: 0.9; margin-top: 5px; }");

            // Call to action
            sb.AppendLine(".cta-section { text-align: center; margin: 30px 0; }");
            sb.AppendLine(".cta-button { display: inline-block; background: linear-gradient(135deg, #00b894, #00a085); color: white; padding: 15px 30px; border-radius: 50px; text-decoration: none; font-weight: 600; font-size: 16px; transition: all 0.3s ease; }");
            sb.AppendLine(".cta-button:hover { transform: translateY(-2px); box-shadow: 0 8px 25px rgba(0,184,148,0.3); }");
            sb.AppendLine(".cta-text { color: #636e72; font-size: 14px; margin-top: 15px; }");

            // Footer
            sb.AppendLine(".footer { background-color: #2d3436; color: #b2bec3; text-align: center; padding: 30px; }");
            sb.AppendLine(".footer .brand { font-size: 18px; font-weight: 600; margin-bottom: 10px; color: white; }");
            sb.AppendLine(".footer .disclaimer { font-size: 13px; opacity: 0.8; }");
            sb.AppendLine(".footer .links { margin-top: 15px; }");
            sb.AppendLine(".footer .links a { color: #74b9ff; text-decoration: none; margin: 0 10px; font-size: 13px; }");

            // Responsive
            sb.AppendLine("@media (max-width: 600px) {");
            sb.AppendLine("  .container { margin: 10px; border-radius: 8px; }");
            sb.AppendLine("  .header, .content, .footer { padding: 20px; }");
            sb.AppendLine("  .header h1 { font-size: 24px; }");
            sb.AppendLine("  .message-item { padding: 15px; }");
            sb.AppendLine("  .sender-avatar { width: 35px; height: 35px; font-size: 14px; }");
            sb.AppendLine("}");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class='email-wrapper'>");
            sb.AppendLine("<div class='container'>");

            // Header section
            sb.AppendLine("<div class='header'>");
            sb.AppendLine("<h1>💬 Bạn có tin nhắn mới!</h1>");
            sb.AppendLine("<p class='subtitle'>Real Estate Platform</p>");
            sb.AppendLine("</div>");

            // Content section
            sb.AppendLine("<div class='content'>");
            sb.AppendLine($"<div class='greeting'>Xin chào <span class='username'>{userName}</span>! 👋</div>");

            // Summary box
            sb.AppendLine("<div class='summary'>");
            sb.AppendLine($"<h3>{unreadMessages.Count} tin nhắn chưa đọc</h3>");
            sb.AppendLine("<p>Dưới đây là những tin nhắn gần đây nhất</p>");
            sb.AppendLine("</div>");

            // Messages
            sb.AppendLine("<div class='messages-container'>");
            foreach (var message in unreadMessages.Take(10))
            {
                var senderInitial = !string.IsNullOrEmpty(message.SenderFullName) ? message.SenderFullName[0].ToString().ToUpper() : "?";
                var timeAgo = GetTimeAgo(message.SentAt);

                sb.AppendLine("<div class='message-item'>");
                sb.AppendLine("<div class='sender-info'>");
                sb.AppendLine($"<div class='sender-avatar'>{senderInitial}</div>");
                sb.AppendLine("<div class='sender-details'>");
                sb.AppendLine($"<h4>{message.SenderFullName}</h4>");
                sb.AppendLine($"<div class='time'>🕒 {timeAgo} • {message.SentAt:HH:mm}</div>");
                sb.AppendLine("</div>");
                sb.AppendLine("</div>");
                sb.AppendLine($"<div class='message-content'>{message.Content}</div>");
                sb.AppendLine("</div>");
            }
            sb.AppendLine("</div>");

            // More messages indicator
            if (unreadMessages.Count > 10)
            {
                sb.AppendLine("<div class='more-messages'>");
                sb.AppendLine($"<span class='count'>+{unreadMessages.Count - 10}</span>");
                sb.AppendLine("<div class='text'>tin nhắn khác đang chờ bạn</div>");
                sb.AppendLine("</div>");
            }
            var baseUrl = GetBaseUrl();
            var chatUrl = $"{baseUrl}/chat";
            // Call to action
            sb.AppendLine("<div class='cta-section'>");
            sb.AppendLine($"<a href='{chatUrl}' class='cta-button'>");
            sb.AppendLine("<img src='https://fonts.gstatic.com/s/e/notoemoji/16.0/1f680/32.png' alt='🚀' style='height:16px; vertical-align:middle;'> Xem tất cả tin nhắn");
            sb.AppendLine("</a>");
            sb.AppendLine("<div class='cta-text'>Nhấn vào đây để đăng nhập và trả lời tin nhắn</div>");
            sb.AppendLine("</div>");

            // Footer
            sb.AppendLine("<div class='footer'>");
            sb.AppendLine("<div class='brand'>🏠 Real Estate Platform</div>");
            sb.AppendLine("<div class='disclaimer'>Đây là email tự động, vui lòng không trả lời trực tiếp email này.</div>");
            sb.AppendLine("<div class='links'>");
            sb.AppendLine("<a href='#'>Trung tâm trợ giúp</a>");
            sb.AppendLine("<a href='#'>Chính sách bảo mật</a>");
            sb.AppendLine("<a href='#'>Hủy đăng ký</a>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");

            sb.AppendLine("</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        private string GetTimeAgo(DateTime sentTime)
        {
            var timeSpan = DateTime.Now - sentTime;

            if (timeSpan.TotalMinutes < 1)
                return "vừa xong";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} phút trước";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} giờ trước";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} ngày trước";

            return sentTime.ToString("dd/MM/yyyy");
        }

        public async Task SendPasswordResetEmailAsync(string userEmail, string userName, string resetToken)
        {
            try
            {
                using var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(Email, Password)
                };

                var resetUrl = $"{GetBaseUrl()}/reset-password?token={resetToken}";

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(Email, "Real Estate Platform"),
                    Subject = "Khôi phục mật khẩu",
                    Body = GeneratePasswordResetEmailBody(userName, resetUrl, resetToken),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(userEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Password reset email sent successfully to {userEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send password reset email to {userEmail}");
                throw;
            }
        }
        private string GeneratePasswordResetEmailBody(string userName, string resetUrl, string resetToken)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'>");
            sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }");
            sb.AppendLine(".container { max-width: 600px; margin: 0 auto; background-color: white; }");
            sb.AppendLine(".header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px 20px; text-align: center; }");
            sb.AppendLine(".content { padding: 30px 20px; }");
            sb.AppendLine(".reset-button { display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; }");
            sb.AppendLine(".reset-button:hover { opacity: 0.9; }");
            sb.AppendLine(".warning { background-color: #fff3cd; border: 1px solid #ffeaa7; color: #856404; padding: 15px; border-radius: 5px; margin: 20px 0; }");
            sb.AppendLine(".footer { background-color: #f8f9fa; text-align: center; padding: 20px; color: #666; font-size: 14px; }");
            sb.AppendLine(".token-box { background-color: #f8f9fa; border: 1px solid #dee2e6; padding: 15px; border-radius: 5px; font-family: monospace; word-break: break-all; margin: 15px 0; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class='container'>");

            // Header
            sb.AppendLine("<div class='header'>");
            sb.AppendLine("<h1>🔒 Khôi phục mật khẩu</h1>");
            sb.AppendLine("<p>Bất Động Sản Long An</p>");
            sb.AppendLine("</div>");

            // Content
            sb.AppendLine("<div class='content'>");
            sb.AppendLine($"<h2>Xin chào {userName},</h2>");
            sb.AppendLine("<p>Chúng tôi nhận được yêu cầu khôi phục mật khẩu cho tài khoản của bạn. Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này.</p>");

            sb.AppendLine("<p>Để đặt lại mật khẩu, vui lòng nhấn vào nút bên dưới:</p>");
            sb.AppendLine($"<div style='text-align: center;'>");
            sb.AppendLine($"<a href='{resetUrl}' class='reset-button'>Đặt lại mật khẩu</a>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='warning'>");
            sb.AppendLine("<strong>⚠️ Lưu ý quan trọng:</strong>");
            sb.AppendLine("<ul>");
            sb.AppendLine("<li>Link này chỉ có hiệu lực trong <strong>60 phút</strong></li>");
            sb.AppendLine("<li>Chỉ sử dụng được <strong>một lần</strong></li>");
            sb.AppendLine("<li>Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("</div>");

            sb.AppendLine("<p>Nếu nút bên trên không hoạt động, bạn có thể copy và dán link sau vào trình duyệt:</p>");
            sb.AppendLine($"<div class='token-box'>{resetUrl}</div>");

            sb.AppendLine("<p>Hoặc sử dụng mã token sau trên trang đặt lại mật khẩu:</p>");
            sb.AppendLine($"<div class='token-box'>{resetToken}</div>");

            sb.AppendLine("<p>Nếu bạn cần hỗ trợ, vui lòng liên hệ với chúng tôi.</p>");
            sb.AppendLine("<p>Trân trọng,<br><strong>Đội ngũ Bất Động Sản Long An</strong></p>");
            sb.AppendLine("</div>");

            // Footer
            sb.AppendLine("<div class='footer'>");
            sb.AppendLine("<p>© 2025 Bất Động Sản Long An. All rights reserved.</p>");
            sb.AppendLine("<p>Đây là email tự động, vui lòng không trả lời email này.</p>");
            sb.AppendLine($"<p><small>Email được gửi vào lúc {DateTime.Now:dd/MM/yyyy HH:mm:ss}</small></p>");
            sb.AppendLine("</div>");

            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        private string GetBaseUrl()
        {
            // Lấy base URL từ configuration hoặc environment
            return _configuration["AppSettings:BaseUrl"] ?? "https://localhost:5001";
        }
        public async Task SendEmailConfirmationAsync(string userEmail, string userName, string confirmationToken)
        {
            try
            {
                using var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(Email, Password)
                };

                var confirmationUrl = $"{GetBaseUrl()}/confirm-email?token={confirmationToken}";

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(Email, "Real Estate Platform"),
                    Subject = "Xác nhận địa chỉ email của bạn",
                    Body = GenerateEmailConfirmationBody(userName, confirmationUrl, confirmationToken),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(userEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Email confirmation sent successfully to {userEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email confirmation to {userEmail}");
                throw;
            }
        }

        private string GenerateEmailConfirmationBody(string userName, string confirmationUrl, string confirmationToken)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'>");
            sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.AppendLine("<title>Xác nhận Email</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }");
            sb.AppendLine(".container { max-width: 600px; margin: 0 auto; background-color: white; }");
            sb.AppendLine(".header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px 20px; text-align: center; }");
            sb.AppendLine(".content { padding: 30px 20px; }");
            sb.AppendLine(".confirm-button { display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; }");
            sb.AppendLine(".confirm-button:hover { opacity: 0.9; }");
            sb.AppendLine(".warning { background-color: #fff3cd; border: 1px solid #ffeaa7; color: #856404; padding: 15px; border-radius: 5px; margin: 20px 0; }");
            sb.AppendLine(".footer { background-color: #f8f9fa; text-align: center; padding: 20px; color: #666; font-size: 14px; }");
            sb.AppendLine(".token-box { background-color: #f8f9fa; border: 1px solid #dee2e6; padding: 15px; border-radius: 5px; font-family: monospace; word-break: break-all; margin: 15px 0; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class='container'>");

            // Header
            sb.AppendLine("<div class='header'>");
            sb.AppendLine("<h1>🎉 Chào mừng bạn đến với Real Estate Platform!</h1>");
            sb.AppendLine("</div>");

            // Content
            sb.AppendLine("<div class='content'>");
            sb.AppendLine($"<h2>Xin chào {userName},</h2>");
            sb.AppendLine("<p>Cảm ơn bạn đã đăng ký tài khoản tại Real Estate Platform. Để hoàn tất quá trình đăng ký, vui lòng xác nhận địa chỉ email của bạn bằng cách nhấn vào nút bên dưới:</p>");

            sb.AppendLine($"<div style='text-align: center;'>");
            sb.AppendLine($"<a href='{confirmationUrl}' class='confirm-button'>Xác nhận Email</a>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='warning'>");
            sb.AppendLine("<strong>⚠️ Lưu ý quan trọng:</strong>");
            sb.AppendLine("<ul>");
            sb.AppendLine("<li>Link này chỉ có hiệu lực trong <strong>24 giờ</strong></li>");
            sb.AppendLine("<li>Tài khoản của bạn sẽ bị hạn chế một số chức năng cho đến khi email được xác nhận</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("</div>");

            sb.AppendLine("<p>Nếu nút bên trên không hoạt động, bạn có thể copy và dán link sau vào trình duyệt:</p>");
            sb.AppendLine($"<div class='token-box'>{confirmationUrl}</div>");

            sb.AppendLine("<p>Trân trọng,<br><strong>Đội ngũ Real Estate Platform</strong></p>");
            sb.AppendLine("</div>");

            // Footer
            sb.AppendLine("<div class='footer'>");
            sb.AppendLine("<p>© 2025 Real Estate Platform. All rights reserved.</p>");
            sb.AppendLine("<p>Đây là email tự động, vui lòng không trả lời email này.</p>");
            sb.AppendLine($"<p><small>Email được gửi vào lúc {DateTime.Now:dd/MM/yyyy HH:mm:ss}</small></p>");
            sb.AppendLine("</div>");

            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
        // Thêm các method này vào EmailService class

        public async Task SendVipExpiredNotificationAsync(string userEmail, string userName, Property property)
        {
            try
            {
                using var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(Email, Password)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(Email, "Real Estate Platform"),
                    Subject = "🔔 Gói VIP của bạn đã hết hạn",
                    Body = GenerateVipExpiredEmailBody(userName, property),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(userEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"VIP expired email sent successfully to {userEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send VIP expired email to {userEmail}");
                throw;
            }
        }

        public async Task SendVipExpiringNotificationAsync(string userEmail, string userName, Property property, int daysLeft)
        {
            try
            {
                using var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(Email, Password)
                };

                var subject = daysLeft switch
                {
                    1 => "⚠️ Gói VIP sẽ hết hạn trong 1 ngày!",
                    _ => $"⏰ Gói VIP sẽ hết hạn trong {daysLeft} ngày"
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(Email, "Real Estate Platform"),
                    Subject = subject,
                    Body = GenerateVipExpiringEmailBody(userName, property, daysLeft),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(userEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"VIP expiring email sent successfully to {userEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send VIP expiring email to {userEmail}");
                throw;
            }
        }

        private string GenerateVipExpiredEmailBody(string userName, Property property)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'>");
            sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.AppendLine("<title>Gói VIP đã hết hạn</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; background-color: #f8f9fa; margin: 0; padding: 0; }");
            sb.AppendLine(".container { max-width: 650px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 8px 32px rgba(0,0,0,0.1); }");
            sb.AppendLine(".header { background: linear-gradient(135deg, #e74c3c 0%, #c0392b 100%); color: white; padding: 40px 30px; text-align: center; position: relative; }");
            sb.AppendLine(".header h1 { font-size: 28px; font-weight: 600; margin-bottom: 8px; }");
            sb.AppendLine(".content { padding: 40px 30px; }");
            sb.AppendLine(".property-card { background: #fff; border: 2px solid #e74c3c; border-radius: 12px; padding: 25px; margin: 25px 0; position: relative; }");
            sb.AppendLine(".property-card::before { content: '⚠️'; position: absolute; top: -15px; left: 25px; background: #e74c3c; color: white; padding: 8px 12px; border-radius: 20px; font-size: 16px; }");
            sb.AppendLine(".property-title { font-size: 20px; font-weight: 600; color: #2d3436; margin-bottom: 10px; padding-left: 20px; }");
            sb.AppendLine(".property-details { color: #636e72; font-size: 14px; }");
            sb.AppendLine(".info-box { background: linear-gradient(135deg, #fdcb6e, #e17055); color: white; padding: 20px; border-radius: 10px; margin: 25px 0; text-align: center; }");
            sb.AppendLine(".renew-button { display: inline-block; background: linear-gradient(135deg, #00b894, #00a085); color: white; padding: 15px 30px; text-decoration: none; border-radius: 50px; font-weight: 600; font-size: 16px; margin: 20px 0; }");
            sb.AppendLine(".renew-button:hover { transform: translateY(-2px); box-shadow: 0 8px 25px rgba(0,184,148,0.3); }");
            sb.AppendLine(".footer { background-color: #2d3436; color: #b2bec3; text-align: center; padding: 30px; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class='container'>");

            // Header
            sb.AppendLine("<div class='header'>");
            sb.AppendLine("<h1>🔔 Gói VIP đã hết hạn</h1>");
            sb.AppendLine("<p>Real Estate Platform</p>");
            sb.AppendLine("</div>");

            // Content
            sb.AppendLine("<div class='content'>");
            sb.AppendLine($"<h2>Xin chào {userName},</h2>");
            sb.AppendLine("<p>Chúng tôi thông báo rằng gói VIP cho tin đăng của bạn đã hết hạn.</p>");

            // Property info
            sb.AppendLine("<div class='property-card'>");
            sb.AppendLine($"<div class='property-title'>{property.Title}</div>");
            sb.AppendLine("<div class='property-details'>");
            sb.AppendLine($"<p><strong>Địa chỉ:</strong> {property.Address ?? "Chưa cập nhật"}</p>");
            sb.AppendLine($"<p><strong>Giá:</strong> {(property.Price.HasValue ? property.Price.Value.ToString("N0") + " VNĐ" : "Thỏa thuận")}</p>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='info-box'>");
            sb.AppendLine("<h3>📊 Điều gì xảy ra khi VIP hết hạn?</h3>");
            sb.AppendLine("<ul style='text-align: left; margin-top: 15px;'>");
            sb.AppendLine("<li>Tin đăng không còn hiển thị ưu tiên</li>");
            sb.AppendLine("<li>Không còn hiển thị nhãn VIP</li>");
            sb.AppendLine("<li>Giảm khả năng tiếp cận khách hàng</li>");
            sb.AppendLine("<li>Tin đăng vẫn hoạt động bình thường</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("</div>");

            var baseUrl = GetBaseUrl();
            sb.AppendLine("<div style='text-align: center;'>");
            sb.AppendLine($"<a href='{baseUrl}/UserPropertiesPage' class='renew-button'>");
            sb.AppendLine("🚀 Gia hạn VIP ngay");
            sb.AppendLine("</a>");
            sb.AppendLine("</div>");

            sb.AppendLine("<p>Để tin đăng của bạn tiếp tục được ưu tiên hiển thị và tiếp cận nhiều khách hàng hơn, hãy gia hạn gói VIP ngay hôm nay!</p>");
            sb.AppendLine("<p>Trân trọng,<br><strong>Đội ngũ Real Estate Platform</strong></p>");
            sb.AppendLine("</div>");

            // Footer
            sb.AppendLine("<div class='footer'>");
            sb.AppendLine("<p>© 2025 Real Estate Platform. All rights reserved.</p>");
            sb.AppendLine("<p>Đây là email tự động, vui lòng không trả lời email này.</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        private string GenerateVipExpiringEmailBody(string userName, Property property, int daysLeft)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'>");
            sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.AppendLine("<title>Gói VIP sắp hết hạn</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; background-color: #f8f9fa; margin: 0; padding: 0; }");
            sb.AppendLine(".container { max-width: 650px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 8px 32px rgba(0,0,0,0.1); }");

            var headerColor = daysLeft == 1 ? "#e74c3c, #c0392b" : "#f39c12, #e67e22";
            sb.AppendLine($".header {{ background: linear-gradient(135deg, {headerColor}); color: white; padding: 40px 30px; text-align: center; position: relative; }}");
            sb.AppendLine(".header h1 { font-size: 28px; font-weight: 600; margin-bottom: 8px; }");
            sb.AppendLine(".content { padding: 40px 30px; }");
            sb.AppendLine(".countdown-box { background: linear-gradient(135deg, #fd79a8, #e84393); color: white; padding: 30px; border-radius: 15px; text-align: center; margin: 25px 0; }");
            sb.AppendLine(".countdown-number { font-size: 48px; font-weight: 700; margin-bottom: 10px; }");
            sb.AppendLine(".property-card { background: #fff; border: 2px solid #f39c12; border-radius: 12px; padding: 25px; margin: 25px 0; }");
            sb.AppendLine(".property-title { font-size: 20px; font-weight: 600; color: #2d3436; margin-bottom: 10px; }");
            sb.AppendLine(".property-details { color: #636e72; font-size: 14px; }");
            sb.AppendLine(".renew-button { display: inline-block; background: linear-gradient(135deg, #00b894, #00a085); color: white; padding: 15px 30px; text-decoration: none; border-radius: 50px; font-weight: 600; font-size: 16px; margin: 20px 0; }");
            sb.AppendLine(".footer { background-color: #2d3436; color: #b2bec3; text-align: center; padding: 30px; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class='container'>");

            // Header
            sb.AppendLine("<div class='header'>");
            var urgencyIcon = daysLeft == 1 ? "⚠️" : "⏰";
            sb.AppendLine($"<h1>{urgencyIcon} Gói VIP sắp hết hạn</h1>");
            sb.AppendLine("<p>Real Estate Platform</p>");
            sb.AppendLine("</div>");

            // Content
            sb.AppendLine("<div class='content'>");
            sb.AppendLine($"<h2>Xin chào {userName},</h2>");

            var urgencyMessage = daysLeft switch
            {
                1 => "Gói VIP của bạn sẽ hết hạn trong vòng 24 giờ tới!",
                _ => $"Gói VIP của bạn sẽ hết hạn trong {daysLeft} ngày nữa."
            };

            sb.AppendLine($"<p>{urgencyMessage}</p>");

            // Countdown
            sb.AppendLine("<div class='countdown-box'>");
            sb.AppendLine($"<div class='countdown-number'>{daysLeft}</div>");
            sb.AppendLine($"<div>ngày còn lại</div>");
            sb.AppendLine("</div>");

            // Property info
            sb.AppendLine("<div class='property-card'>");
            sb.AppendLine($"<div class='property-title'>🏠 {property.Title}</div>");
            sb.AppendLine("<div class='property-details'>");
            sb.AppendLine($"<p><strong>Địa chỉ:</strong> {property.Address ?? "Chưa cập nhật"}</p>");
            sb.AppendLine($"<p><strong>Giá:</strong> {(property.Price.HasValue ? property.Price.Value.ToString("N0") + " VNĐ" : "Thỏa thuận")}</p>");
            sb.AppendLine($"<p><strong>Ngày hết hạn VIP:</strong> {property.VipEndDate?.ToString("dd/MM/yyyy HH:mm")}</p>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");

            var baseUrl = GetBaseUrl();
            sb.AppendLine("<div style='text-align: center;'>");
            sb.AppendLine($"<a href='{baseUrl}/UserPropertiesPage' class='renew-button'>");
            sb.AppendLine("🚀 Gia hạn VIP ngay");
            sb.AppendLine("</a>");
            sb.AppendLine("</div>");

            sb.AppendLine("<p>Đừng để tin đăng của bạn mất đi lợi thế cạnh tranh! Gia hạn VIP ngay để tiếp tục được ưu tiên hiển thị.</p>");
            sb.AppendLine("<p>Trân trọng,<br><strong>Đội ngũ Real Estate Platform</strong></p>");
            sb.AppendLine("</div>");

            // Footer
            sb.AppendLine("<div class='footer'>");
            sb.AppendLine("<p>© 2025 Real Estate Platform. All rights reserved.</p>");
            sb.AppendLine("<p>Đây là email tự động, vui lòng không trả lời email này.</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
        // Thêm các method này vào class EmailService hiện tại

        public async Task SendSuspiciousActivityNotificationAsync(string userEmail, string userName, string suspiciousIP, DateTime lastLoginTime)
        {
            try
            {
                using var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(Email, Password)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(Email, "Real Estate Platform Security"),
                    Subject = "🚨 Cảnh báo bảo mật: Phát hiện hoạt động đăng nhập bất thường",
                    Body = GenerateSuspiciousActivityEmailBody(userName, suspiciousIP, lastLoginTime),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(userEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Suspicious activity notification sent successfully to {userEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send suspicious activity notification to {userEmail}");
                throw;
            }
        }

        public async Task SendAdminSuspiciousActivityReportAsync(string adminEmail, object suspiciousActivityReport)
        {
            try
            {
                using var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(Email, Password)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(Email, "Real Estate Platform Security"),
                    Subject = "🚨 SECURITY ALERT: Suspicious IP Auto-Banned",
                    Body = GenerateAdminSecurityReportBody(suspiciousActivityReport),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(adminEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Admin security report sent successfully to {adminEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send admin security report to {adminEmail}");
                throw;
            }
        }

        private string GenerateSuspiciousActivityEmailBody(string userName, string suspiciousIP, DateTime lastLoginTime)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'>");
            sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.AppendLine("<title>Cảnh báo bảo mật</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; background-color: #f8f9fa; margin: 0; padding: 0; }");
            sb.AppendLine(".container { max-width: 650px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 8px 32px rgba(0,0,0,0.1); }");
            sb.AppendLine(".header { background: linear-gradient(135deg, #e74c3c 0%, #c0392b 100%); color: white; padding: 40px 30px; text-align: center; position: relative; }");
            sb.AppendLine(".header::before { content: '🚨'; position: absolute; top: 20px; right: 30px; font-size: 30px; }");
            sb.AppendLine(".header h1 { font-size: 28px; font-weight: 600; margin-bottom: 8px; }");
            sb.AppendLine(".content { padding: 40px 30px; }");
            sb.AppendLine(".warning-box { background: linear-gradient(135deg, #ff7675, #d63031); color: white; padding: 25px; border-radius: 10px; margin: 25px 0; text-align: center; }");
            sb.AppendLine(".info-box { background: #f8f9fa; border-left: 4px solid #e74c3c; padding: 20px; margin: 20px 0; }");
            sb.AppendLine(".action-box { background: linear-gradient(135deg, #74b9ff, #0984e3); color: white; padding: 20px; border-radius: 10px; margin: 25px 0; }");
            sb.AppendLine(".security-button { display: inline-block; background: linear-gradient(135deg, #fdcb6e, #e17055); color: white; padding: 15px 30px; text-decoration: none; border-radius: 50px; font-weight: 600; font-size: 16px; margin: 10px 5px; }");
            sb.AppendLine(".footer { background-color: #2d3436; color: #b2bec3; text-align: center; padding: 30px; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class='container'>");

            // Header
            sb.AppendLine("<div class='header'>");
            sb.AppendLine("<h1>Cảnh báo bảo mật</h1>");
            sb.AppendLine("<p>Phát hiện hoạt động đăng nhập bất thường</p>");
            sb.AppendLine("</div>");

            // Content
            sb.AppendLine("<div class='content'>");
            sb.AppendLine($"<h2>Xin chào {userName},</h2>");
            sb.AppendLine("<p>Chúng tôi đã phát hiện hoạt động đăng nhập bất thường liên quan đến tài khoản của bạn và đã thực hiện các biện pháp bảo mật.</p>");

            sb.AppendLine("<div class='warning-box'>");
            sb.AppendLine("<h3>⚠️ Thông tin bảo mật</h3>");
            sb.AppendLine("<p>Một địa chỉ IP đã được tự động chặn do có hành vi đăng nhập đáng ngờ</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='info-box'>");
            sb.AppendLine("<h4>Chi tiết:</h4>");
            sb.AppendLine($"<p><strong>IP bị chặn:</strong> {suspiciousIP}</p>");
            sb.AppendLine($"<p><strong>Lần đăng nhập cuối:</strong> {lastLoginTime:dd/MM/yyyy HH:mm:ss}</p>");
            sb.AppendLine($"<p><strong>Lý do:</strong> IP này đã đăng nhập quá nhiều tài khoản khác nhau trong thời gian ngắn</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='action-box'>");
            sb.AppendLine("<h3>🛡️ Điều gì đã được thực hiện?</h3>");
            sb.AppendLine("<ul>");
            sb.AppendLine("<li>IP đã được tự động chặn khỏi hệ thống</li>");
            sb.AppendLine("<li>Tài khoản của bạn vẫn an toàn và hoạt động bình thường</li>");
            sb.AppendLine("<li>Tất cả hoạt động đăng nhập đã được ghi lại</li>");
            sb.AppendLine("</ul>");
            sb.AppendLine("</div>");

            var baseUrl = GetBaseUrl();
            sb.AppendLine("<h3>🔐 Khuyến nghị bảo mật:</h3>");
            sb.AppendLine("<div style='text-align: center; margin: 30px 0;'>");
            sb.AppendLine($"<a href='{baseUrl}/Profile' class='security-button'>Đổi mật khẩu</a>");
            sb.AppendLine("</div>");

            sb.AppendLine("<ul>");
            sb.AppendLine("<li>Thay đổi mật khẩu ngay lập tức nếu bạn nghi ngờ tài khoản bị xâm phạm</li>");
            sb.AppendLine("<li>Không chia sẻ thông tin đăng nhập với bất kỳ ai</li>");
            sb.AppendLine("<li>Sử dụng mật khẩu mạnh và duy nhất</li>");
            sb.AppendLine("<li>Kiểm tra hoạt động đăng nhập thường xuyên</li>");
            sb.AppendLine("</ul>");

            sb.AppendLine("<p><strong>Nếu bạn không thực hiện các hoạt động đăng nhập này hoặc có thắc mắc, vui lòng liên hệ với chúng tôi ngay lập tức.</strong></p>");
            sb.AppendLine("<p>Trân trọng,<br><strong>Đội ngũ Bảo mật Real Estate Platform</strong></p>");
            sb.AppendLine("</div>");

            // Footer
            sb.AppendLine("<div class='footer'>");
            sb.AppendLine("<p>© 2025 Real Estate Platform. All rights reserved.</p>");
            sb.AppendLine("<p>Email bảo mật tự động - Không trả lời email này</p>");
            sb.AppendLine($"<p><small>Được gửi vào {DateTime.Now:dd/MM/yyyy HH:mm:ss}</small></p>");
            sb.AppendLine("</div>");

            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        private string GenerateAdminSecurityReportBody(object reportData)
        {
            var report = reportData as dynamic;
            var sb = new StringBuilder();

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'>");
            sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.AppendLine("<title>Security Alert Report</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; background-color: #f8f9fa; margin: 0; padding: 0; }");
            sb.AppendLine(".container { max-width: 800px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 8px 32px rgba(0,0,0,0.1); }");
            sb.AppendLine(".header { background: linear-gradient(135deg, #2d3436 0%, #636e72 100%); color: white; padding: 40px 30px; text-align: center; }");
            sb.AppendLine(".content { padding: 40px 30px; }");
            sb.AppendLine(".alert-box { background: linear-gradient(135deg, #e17055, #d63031); color: white; padding: 25px; border-radius: 10px; margin: 25px 0; }");
            sb.AppendLine(".stats-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 20px; margin: 25px 0; }");
            sb.AppendLine(".stat-card { background: #f8f9fa; border: 1px solid #dee2e6; padding: 20px; border-radius: 8px; text-align: center; }");
            sb.AppendLine(".stat-number { font-size: 32px; font-weight: 700; color: #e74c3c; }");
            sb.AppendLine(".users-table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
            sb.AppendLine(".users-table th, .users-table td { padding: 12px; border: 1px solid #dee2e6; text-align: left; }");
            sb.AppendLine(".users-table th { background-color: #f8f9fa; font-weight: 600; }");
            sb.AppendLine(".ip-highlight { background: #fff3cd; padding: 3px 8px; border-radius: 4px; font-family: monospace; }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class='container'>");

            // Header
            sb.AppendLine("<div class='header'>");
            sb.AppendLine("<h1>🚨 SECURITY ALERT</h1>");
            sb.AppendLine("<p>Automatic IP Ban Report</p>");
            sb.AppendLine($"<p><small>{DateTime.Now:dd/MM/yyyy HH:mm:ss}</small></p>");
            sb.AppendLine("</div>");

            // Content
            sb.AppendLine("<div class='content'>");
            sb.AppendLine("<div class='alert-box'>");
            sb.AppendLine("<h2>⚠️ Suspicious IP Automatically Banned</h2>");
            sb.AppendLine($"<p>IP Address: <span class='ip-highlight'>{report.IPAddress}</span> has been automatically banned due to suspicious login activity.</p>");
            sb.AppendLine("</div>");

            // Statistics
            sb.AppendLine("<div class='stats-grid'>");
            sb.AppendLine("<div class='stat-card'>");
            sb.AppendLine("<div class='stat-number'>" + report.UniqueUserCount + "</div>");
            sb.AppendLine("<div>Unique Accounts</div>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='stat-card'>");
            sb.AppendLine("<div class='stat-number'>" + report.TotalLoginCount + "</div>");
            sb.AppendLine("<div>Total Login Attempts</div>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='stat-card'>");
            sb.AppendLine($"<div class='stat-number'>1</div>");
            sb.AppendLine("<div>Hour Window</div>");
            sb.AppendLine("</div>");
            sb.AppendLine("</div>");

            // Timeline
            sb.AppendLine("<h3>📅 Activity Timeline</h3>");
            sb.AppendLine("<p><strong>First Login:</strong> " + ((DateTime)report.FirstLogin).ToString("dd/MM/yyyy HH:mm:ss") + "</p>");
            sb.AppendLine("<p><strong>Last Login:</strong> " + ((DateTime)report.LastLogin).ToString("dd/MM/yyyy HH:mm:ss") + "</p>");
            sb.AppendLine("<p><strong>Duration:</strong> " + ((DateTime)report.LastLogin - (DateTime)report.FirstLogin).TotalMinutes.ToString("F0") + " minutes</p>");

            // Affected Users Table
            sb.AppendLine("<h3>👥 Affected User Accounts</h3>");
            sb.AppendLine("<table class='users-table'>");
            sb.AppendLine("<thead>");
            sb.AppendLine("<tr>");
            sb.AppendLine("<th>User ID</th>");
            sb.AppendLine("<th>Username</th>");
            sb.AppendLine("<th>Full Name</th>");
            sb.AppendLine("<th>Email</th>");
            sb.AppendLine("</tr>");
            sb.AppendLine("</thead>");
            sb.AppendLine("<tbody>");

            // Lặp qua danh sách users bị ảnh hưởng
            var affectedUsers = report.AffectedUsers as IEnumerable<dynamic>;
            if (affectedUsers != null)
            {
                foreach (var user in affectedUsers)
                {
                    sb.AppendLine("<tr>");
                    sb.AppendLine($"<td>{user.Id}</td>");
                    sb.AppendLine($"<td>{user.UserName ?? "N/A"}</td>");
                    sb.AppendLine($"<td>{user.FullName ?? "N/A"}</td>");
                    sb.AppendLine($"<td>{user.Email ?? "N/A"}</td>");
                    sb.AppendLine("</tr>");
                }
            }

            sb.AppendLine("</tbody>");
            sb.AppendLine("</table>");

            // Actions Taken
            sb.AppendLine("<h3>🛡️ Actions Taken</h3>");
            sb.AppendLine("<ul>");
            sb.AppendLine("<li>✅ IP address has been added to the banned list</li>");
            sb.AppendLine("<li>✅ All affected users have been notified via email</li>");
            sb.AppendLine("<li>✅ Security event has been logged</li>");
            sb.AppendLine("<li>📧 This admin report has been generated</li>");
            sb.AppendLine("</ul>");

            // Recommendations
            sb.AppendLine("<h3>💡 Recommendations</h3>");
            sb.AppendLine("<ul>");
            sb.AppendLine("<li>Monitor the affected user accounts for any unusual activity</li>");
            sb.AppendLine("<li>Consider reviewing and updating security policies</li>");
            sb.AppendLine("<li>Check if this IP needs to be whitelisted (if it's a legitimate shared connection)</li>");
            sb.AppendLine("<li>Review login patterns for additional security measures</li>");
            sb.AppendLine("</ul>");

            sb.AppendLine("<p><strong>This is an automated security report. Please review and take appropriate action if necessary.</strong></p>");
            sb.AppendLine("</div>");

            sb.AppendLine("</div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
        public async Task SendPropertyRecommendationsAsync(string userEmail, string userName, List<Property> recommendations)
        {
            try
            {
                using var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(Email, Password)
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(Email, "Real Estate Platform"),
                    Subject = "🏠 Gợi ý bất động sản phù hợp với bạn",
                    Body = GeneratePropertyRecommendationsEmailBody(userName, recommendations),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(userEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation($"Property recommendations email sent successfully to {userEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send property recommendations email to {userEmail}");
                throw;
            }
        }

        private string GeneratePropertyRecommendationsEmailBody(string userName, List<Property> recommendations)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='utf-8'>");
            sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.AppendLine("<title>Gợi ý bất động sản</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("* { margin: 0; padding: 0; box-sizing: border-box; }");
            sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; background-color: #f8f9fa; }");
            sb.AppendLine(".email-wrapper { background-color: #f8f9fa; padding: 20px 0; min-height: 100vh; }");
            sb.AppendLine(".container { max-width: 650px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 8px 32px rgba(0,0,0,0.1); }");

            // Header với gradient đẹp
            sb.AppendLine(".header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 40px 30px; text-align: center; position: relative; }");
            sb.AppendLine(".header::before { content: ''; position: absolute; top: 0; left: 0; right: 0; bottom: 0; background: url('data:image/svg+xml,<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 100 100\"><defs><pattern id=\"grain\" width=\"100\" height=\"100\" patternUnits=\"userSpaceOnUse\"><circle cx=\"25\" cy=\"25\" r=\"1\" fill=\"%23ffffff\" opacity=\"0.1\"/><circle cx=\"75\" cy=\"75\" r=\"1\" fill=\"%23ffffff\" opacity=\"0.05\"/><circle cx=\"50\" cy=\"10\" r=\"0.5\" fill=\"%23ffffff\" opacity=\"0.1\"/></pattern></defs><rect width=\"100\" height=\"100\" fill=\"url(%23grain)\"/></svg>') repeat; }");
            sb.AppendLine(".header h1 { font-size: 28px; font-weight: 600; margin-bottom: 8px; position: relative; z-index: 1; }");
            sb.AppendLine(".header .subtitle { font-size: 16px; opacity: 0.9; position: relative; z-index: 1; }");

            // Content area
            sb.AppendLine(".content { padding: 40px 30px; }");
            sb.AppendLine(".greeting { font-size: 18px; color: #2d3436; margin-bottom: 25px; }");
            sb.AppendLine(".greeting .username { color: #6c5ce7; font-weight: 600; }");
            sb.AppendLine(".intro-text { color: #636e72; font-size: 16px; margin-bottom: 30px; text-align: center; }");

            // Property cards
            sb.AppendLine(".properties-grid { margin-bottom: 30px; }");
            sb.AppendLine(".property-card { background: #ffffff; margin: 20px 0; border-radius: 12px; overflow: hidden; border: 1px solid #e9ecef; transition: all 0.3s ease; position: relative; }");
            sb.AppendLine(".property-card:hover { box-shadow: 0 8px 30px rgba(0,0,0,0.12); transform: translateY(-4px); }");
            sb.AppendLine(".property-image { width: 100%; height: 200px; background: linear-gradient(135deg, #74b9ff, #0984e3); display: flex; align-items: center; justify-content: center; color: white; font-size: 48px; position: relative; }");
            sb.AppendLine(".vip-badge { position: absolute; top: 10px; right: 10px; background: linear-gradient(135deg, #fdcb6e, #e17055); color: white; padding: 5px 10px; border-radius: 15px; font-size: 12px; font-weight: 600; }");
            sb.AppendLine(".property-content { padding: 20px; }");
            sb.AppendLine(".property-title { font-size: 18px; font-weight: 600; color: #2d3436; margin-bottom: 10px; line-height: 1.3; }");
            sb.AppendLine(".property-price { font-size: 20px; font-weight: 700; color: #00b894; margin-bottom: 10px; }");
            sb.AppendLine(".property-address { color: #636e72; font-size: 14px; margin-bottom: 15px; display: flex; align-items: center; }");
            sb.AppendLine(".property-details { display: flex; gap: 15px; margin-bottom: 15px; }");
            sb.AppendLine(".detail-item { display: flex; align-items: center; color: #636e72; font-size: 14px; }");
            sb.AppendLine(".detail-item .icon { margin-right: 5px; }");
            sb.AppendLine(".property-footer { display: flex; justify-content: space-between; align-items: center; margin-top: 15px; padding-top: 15px; border-top: 1px solid #e9ecef; }");
            sb.AppendLine(".view-button { background: linear-gradient(135deg, #00b894, #00a085); color: white; padding: 8px 16px; text-decoration: none; border-radius: 20px; font-size: 14px; font-weight: 600; transition: all 0.3s ease; }");
            sb.AppendLine(".view-button:hover { transform: translateY(-1px); box-shadow: 0 4px 15px rgba(0,184,148,0.3); }");
            sb.AppendLine(".property-date { color: #636e72; font-size: 12px; }");

            // Call to action
            sb.AppendLine(".cta-section { text-align: center; margin: 30px 0; padding: 30px; background: linear-gradient(135deg, #fd79a8, #e84393); border-radius: 15px; color: white; }");
            sb.AppendLine(".cta-section h3 { font-size: 20px; margin-bottom: 10px; }");
            sb.AppendLine(".cta-section p { opacity: 0.9; margin-bottom: 20px; }");
            sb.AppendLine(".cta-button { display: inline-block; background: rgba(255,255,255,0.2); color: white; padding: 12px 24px; border-radius: 25px; text-decoration: none; font-weight: 600; font-size: 14px; border: 2px solid rgba(255,255,255,0.3); transition: all 0.3s ease; }");
            sb.AppendLine(".cta-button:hover { background: rgba(255,255,255,0.3); transform: translateY(-2px); }");

            // Footer
            sb.AppendLine(".footer { background-color: #2d3436; color: #b2bec3; text-align: center; padding: 30px; }");
            sb.AppendLine(".footer .brand { font-size: 18px; font-weight: 600; margin-bottom: 10px; color: white; }");
            sb.AppendLine(".footer .disclaimer { font-size: 13px; opacity: 0.8; }");

            // Responsive
            sb.AppendLine("@media (max-width: 600px) {");
            sb.AppendLine("  .container { margin: 10px; border-radius: 8px; }");
            sb.AppendLine("  .header, .content, .footer { padding: 20px; }");
            sb.AppendLine("  .header h1 { font-size: 24px; }");
            sb.AppendLine("  .property-card { margin: 15px 0; }");
            sb.AppendLine("  .property-details { flex-direction: column; gap: 8px; }");
            sb.AppendLine("  .property-footer { flex-direction: column; gap: 10px; }");
            sb.AppendLine("}");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<div class='email-wrapper'>");
            sb.AppendLine("<div class='container'>");

            // Header section
            sb.AppendLine("<div class='header'>");
            sb.AppendLine("<h1>🏠 Bất động sản dành cho bạn</h1>");
            sb.AppendLine("<p class='subtitle'>Gợi ý phù hợp với sở thích của bạn</p>");
            sb.AppendLine("</div>");

            // Content section
            sb.AppendLine("<div class='content'>");
            sb.AppendLine($"<div class='greeting'>Xin chào <span class='username'>{userName}</span>! 👋</div>");
            sb.AppendLine("<div class='intro-text'>Dựa trên lịch sử tìm kiếm của bạn, chúng tôi có những gợi ý bất động sản thú vị có thể bạn quan tâm.</div>");

            // Properties grid
            sb.AppendLine("<div class='properties-grid'>");
            var baseUrl = GetBaseUrl();

            foreach (var property in recommendations.Take(6)) // Hiển thị tối đa 6 property
            {
                sb.AppendLine("<div class='property-card'>");

                // Property image placeholder với emoji
                sb.AppendLine("<div class='property-image'>");
                if (property.IsVip)
                    sb.AppendLine("<div class='vip-badge'>⭐ VIP</div>");

                // Get primary image or first available image
                var primaryImage = property.PropertyImages?.FirstOrDefault(img => img.IsPrimary)
                                  ?? property.PropertyImages?.OrderBy(img => img.SortOrder).FirstOrDefault();

                if (primaryImage != null && !string.IsNullOrEmpty(primaryImage.ImageUrl))
                {
                    // Display actual image
                    sb.AppendLine($"<img src='{baseUrl}{primaryImage.ImageUrl}' alt='{property.Title}' style='width: 100%; height: 100%; object-fit: cover;' />");
                }
                else
                {
                    // Fallback to emoji if no image available
                    sb.AppendLine("<div style='display: flex; align-items: center; justify-content: center; height: 100%; font-size: 48px;'>🏠</div>");
                }
                sb.AppendLine("</div>");

           

                sb.AppendLine("<div class='property-content'>");

                // Title
                sb.AppendLine($"<div class='property-title'>{property.Title}</div>");

                // Price
                var priceText = property.Price.HasValue ?
                    $"{property.Price.Value:N0} VNĐ" : "Thỏa thuận";
                sb.AppendLine($"<div class='property-price'>{priceText}</div>");

                // Address
                if (!string.IsNullOrEmpty(property.Address))
                {
                    sb.AppendLine($"<div class='property-address'>");
                    sb.AppendLine($"<span class='icon'>📍</span> {property.Address}");
                    sb.AppendLine("</div>");
                }

                // Details
                sb.AppendLine("<div class='property-details'>");
                if (property.Area.HasValue)
                {
                    sb.AppendLine($"<div class='detail-item'><span class='icon'>📐</span> {property.Area:F1} m²</div>");
                }
                if (property.Bedrooms.HasValue)
                {
                    sb.AppendLine($"<div class='detail-item'><span class='icon'>🛏️</span> {property.Bedrooms} phòng ngủ</div>");
                }
                if (property.Bathrooms.HasValue)
                {
                    sb.AppendLine($"<div class='detail-item'><span class='icon'>🚿</span> {property.Bathrooms} phòng tắm</div>");
                }
                sb.AppendLine("</div>");

                // Footer with button and date
                sb.AppendLine("<div class='property-footer'>");
                sb.AppendLine($"<a href='{baseUrl}/properties/{property.Id}' class='view-button'>Xem chi tiết</a>");
                sb.AppendLine($"<div class='property-date'>🕒 {property.CreatedAt:dd/MM/yyyy}</div>");
                sb.AppendLine("</div>");

                sb.AppendLine("</div>"); // End property-content
                sb.AppendLine("</div>"); // End property-card
            }
            sb.AppendLine("</div>"); // End properties-grid

            // More properties indicator
            if (recommendations.Count > 6)
            {
                sb.AppendLine("<div class='cta-section'>");
                sb.AppendLine($"<h3>Còn {recommendations.Count - 6} bất động sản khác</h3>");
                sb.AppendLine("<p>Khám phá thêm nhiều lựa chọn hấp dẫn khác</p>");
                sb.AppendLine($"<a href='{baseUrl}/properties' class='cta-button'>Xem tất cả</a>");
                sb.AppendLine("</div>");
            }
            else
            {
                sb.AppendLine("<div class='cta-section'>");
                sb.AppendLine("<h3>🔍 Tìm kiếm thêm</h3>");
                sb.AppendLine("<p>Khám phá thêm nhiều bất động sản khác phù hợp với bạn</p>");
                sb.AppendLine($"<a href='{baseUrl}/properties' class='cta-button'>Tìm kiếm ngay</a>");
                sb.AppendLine("</div>");
            }

            sb.AppendLine("</div>"); // End content

            // Footer
            sb.AppendLine("<div class='footer'>");
            sb.AppendLine("<div class='brand'>🏠 Real Estate Platform</div>");
            sb.AppendLine("<div class='disclaimer'>Đây là email tự động dựa trên hoạt động của bạn. Để hủy nhận email này, vui lòng đăng nhập và cập nhật cài đặt thông báo.</div>");
            sb.AppendLine("</div>");

            sb.AppendLine("</div>"); // End container
            sb.AppendLine("</div>"); // End email-wrapper
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}
