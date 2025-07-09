using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using thuctap2025.Data;
using thuctap2025.DTOs;
using thuctap2025.Models;

namespace thuctap2025.Services
{
    public class PasswordResetService : IPasswordResetService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PasswordResetService> _logger;

        public PasswordResetService(
            ApplicationDbContext context,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<PasswordResetService> logger)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ApiResponse> SendPasswordResetEmailAsync(string email)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
                if (user == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Email không tồn tại trong hệ thống"
                    };
                }

                // Vô hiệu hóa các token cũ chưa sử dụng
                var oldTokens = await _context.PasswordResetToken
                    .Where(t => t.UserId == user.Id && !t.IsUsed && t.ExpiresAt > DateTime.Now)
                    .ToListAsync();

                foreach (var oldToken in oldTokens)
                {
                    oldToken.IsUsed = true;
                    oldToken.UsedAt = DateTime.Now;
                }

                // Tạo token mới
                var token = GenerateSecureToken();
                var resetToken = new PasswordResetToken
                {
                    UserId = user.Id,
                    Token = token,
                    ExpiresAt = DateTime.Now.AddHours(1) // Token có hiệu lực 1 giờ
                };

                _context.PasswordResetToken.Add(resetToken);
                await _context.SaveChangesAsync();

                // Gửi email
                await _emailService.SendPasswordResetEmailAsync(user.Email!, user.FullName ?? user.UserName, token);

                _logger.LogInformation($"Password reset email sent to {email}");

                return new ApiResponse
                {
                    Success = true,
                    Message = "Email khôi phục mật khẩu đã được gửi. Vui lòng kiểm tra hộp thư của bạn."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending password reset email to {email}");
                return new ApiResponse
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi gửi email. Vui lòng thử lại sau."
                };
            }
        }

        public async Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            try
            {
                var resetToken = await _context.PasswordResetToken
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Token == request.Token && !t.IsUsed);

                if (resetToken == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Token không hợp lệ hoặc đã được sử dụng"
                    };
                }

                if (resetToken.ExpiresAt < DateTime.Now)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Token đã hết hạn. Vui lòng yêu cầu khôi phục mật khẩu mới."
                    };
                }

                // Cập nhật mật khẩu
                var user = resetToken.User;
                user.PasswordHash = request.NewPassword;

                // Đánh dấu token đã sử dụng
                resetToken.IsUsed = true;
                resetToken.UsedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Password reset successfully for user {user.UserName}");

                return new ApiResponse
                {
                    Success = true,
                    Message = "Mật khẩu đã được cập nhật thành công"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return new ApiResponse
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi đặt lại mật khẩu. Vui lòng thử lại sau."
                };
            }
        }

        public async Task<ApiResponse> ValidateResetTokenAsync(string token)
        {
            try
            {
                var resetToken = await _context.PasswordResetToken
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Token == token && !t.IsUsed);

                if (resetToken == null)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Token không hợp lệ hoặc đã được sử dụng"
                    };
                }

                if (resetToken.ExpiresAt < DateTime.Now)
                {
                    return new ApiResponse
                    {
                        Success = false,
                        Message = "Token đã hết hạn"
                    };
                }

                return new ApiResponse
                {
                    Success = true,
                    Message = "Token hợp lệ",
                    Data = new { UserName = resetToken.User.UserName }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating reset token");
                return new ApiResponse
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi kiểm tra token"
                };
            }
        }

        private string GenerateSecureToken()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[32];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }


    }
}
