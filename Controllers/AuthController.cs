using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using thuctap2025.Data;
using thuctap2025.DTOs;
using thuctap2025.Models;
using thuctap2025.Services;

namespace thuctap2025.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;
        private readonly AuditLogService _auditLogService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;
        public AuthController(ApplicationDbContext context, AuthService authService, AuditLogService auditLogService, IEmailService emailService, ILogger<AuthController> logger)
        {
            _context = context;
            _authService = authService;
            _auditLogService = auditLogService;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Lấy IP address của client
                var clientIP = GetClientIPAddress();

                // Kiểm tra xem IP có bị ban không
                var bannedIP = await _context.BannedIPs
                    .FirstOrDefaultAsync(b => b.IPAddress == clientIP);

                if (bannedIP != null)
                {
                    return Unauthorized(new
                    {
                        message = "IP của bạn đã bị cấm truy cập.",
                        banReason = bannedIP.BanReason,
                        bannedAt = bannedIP.BannedAt
                    });
                }

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserName == request.Username);

                if (user == null || user.PasswordHash != request.Password)
                {
                    // Ghi lại lịch sử đăng nhập thất bại
                    await LogLoginAttempt(request.Username, clientIP, false, "Invalid credentials");

                    return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu!" });
                }

                if (user.AccountStatus == "Suspended")
                {
                    await LogLoginAttempt(user.Id, clientIP, false, "Account suspended");
                    return Unauthorized(new { message = "Tài khoản của bạn đang bị tạm khóa." });
                }

                if (user.AccountStatus == "Banned")
                {
                    await LogLoginAttempt(user.Id, clientIP, false, "Account banned");
                    return Unauthorized(new { message = "Tài khoản của bạn đã bị cấm." });
                }

                // Đăng nhập thành công
                var token = _authService.GenerateJwtToken(user);
                _authService.SetJwtToken(token);

                user.LastLogin = DateTime.Now;

                // Ghi lại lịch sử đăng nhập thành công
                await LogLoginAttempt(user.Id, clientIP, true, "Login successful");

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    token,
                    username = user.UserName,
                    role = user.Role,
                    fullName = user.FullName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login attempt");
                return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest registerModel)
        {
            try
            {
                // Lấy IP address của client
                var clientIP = GetClientIPAddress();

                // Kiểm tra xem IP có bị ban không
                var bannedIP = await _context.BannedIPs
                    .FirstOrDefaultAsync(b => b.IPAddress == clientIP);

                if (bannedIP != null)
                {
                    return Unauthorized(new
                    {
                        message = "IP của bạn đã bị cấm truy cập.",
                        banReason = bannedIP.BanReason,
                        bannedAt = bannedIP.BannedAt
                    });
                }

                if (string.IsNullOrEmpty(registerModel.Username) || string.IsNullOrEmpty(registerModel.PasswordHash))
                {
                    return BadRequest(new { message = "Username và Password là bắt buộc." });
                }

                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == registerModel.Username);
                if (existingUser != null)
                {
                    return Conflict(new { message = "Tên người dùng đã tồn tại." });
                }

                if (!string.IsNullOrEmpty(registerModel.Email))
                {
                    var emailExists = await _context.Users.AnyAsync(u => u.Email == registerModel.Email);
                    if (emailExists)
                    {
                        return Conflict(new { message = "Email đã được sử dụng bởi tài khoản khác." });
                    }
                }

                // Tạo token xác nhận email
                var emailConfirmationToken = Guid.NewGuid().ToString();
                var emailConfirmationTokenExpiry = DateTime.Now.AddHours(24);

                var user = new Users
                {
                    UserName = registerModel.Username,
                    PasswordHash = registerModel.PasswordHash,
                    Email = registerModel.Email,
                    FullName = registerModel.FullName,
                    PhoneNumber = registerModel.PhoneNumber,
                    AccountStatus = "Pending", // Tài khoản chờ xác nhận
                    Role = "User",
                    AvatarUrl = null,
                    CreatedAt = DateTime.Now,
                    EmailConfirmationToken = emailConfirmationToken,
                    EmailConfirmationTokenExpiry = emailConfirmationTokenExpiry,
                    IsEmailConfirmed = false
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Ghi lại lịch sử đăng ký (có thể coi như một loại login history)
                var registrationHistory = new UserLoginHistory
                {
                    UserId = user.Id,
                    IPAddress = clientIP,
                    LoginTime = DateTime.Now,
                    DeviceInfo = GetDeviceInfo() // Thông tin thiết bị nếu có
                };

                _context.UserLoginHistories.Add(registrationHistory);

                if (!string.IsNullOrEmpty(user.Email))
                {
                    try
                    {
                        await _emailService.SendEmailConfirmationAsync(
                            user.Email,
                            user.FullName ?? user.UserName,
                            emailConfirmationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send confirmation email to {Email}", user.Email);
                    }
                }

                await _auditLogService.LogActionAsync(
                    "Register",
                    "Users",
                    user.Id,
                    null,
                    JsonSerializer.Serialize(new
                    {
                        Username = user.UserName,
                        Email = user.Email,
                        IPAddress = clientIP,
                        CreatedAt = user.CreatedAt,
                        Status = "PendingConfirmation"
                    }));

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Đăng ký thành công. Vui lòng kiểm tra email để xác nhận tài khoản.",
                    user.Id
                });
            }
            catch (Exception ex)
            {
                await _auditLogService.LogActionAsync(
                    "RegisterError",
                    "System",
                    null,
                    null,
                    JsonSerializer.Serialize(new
                    {
                        Error = ex.Message,
                        IPAddress = GetClientIPAddress(),
                        StackTrace = ex.StackTrace
                    }));
                return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
            }
        }

        // Phương thức helper để lấy IP address của client
        private string GetClientIPAddress()
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Kiểm tra nếu có proxy hoặc load balancer
            if (HttpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                var forwardedFor = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
                if (!string.IsNullOrEmpty(forwardedFor))
                {
                    ipAddress = forwardedFor.Split(',')[0].Trim();
                }
            }
            else if (HttpContext.Request.Headers.ContainsKey("X-Real-IP"))
            {
                var realIP = HttpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
                if (!string.IsNullOrEmpty(realIP))
                {
                    ipAddress = realIP;
                }
            }

            return ipAddress ?? "Unknown";
        }

        // Phương thức helper để lấy thông tin thiết bị
        private string? GetDeviceInfo()
        {
            var userAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault();
            return userAgent;
        }

        // Phương thức helper để ghi lại lịch sử đăng nhập
        private async Task LogLoginAttempt(int? userId, string ipAddress, bool isSuccessful, string? notes = null)
        {
            try
            {
                if (userId.HasValue)
                {
                    var loginHistory = new UserLoginHistory
                    {
                        UserId = userId.Value,
                        IPAddress = ipAddress,
                        LoginTime = DateTime.Now,
                        DeviceInfo = GetDeviceInfo()
                    };

                    _context.UserLoginHistories.Add(loginHistory);
                }

                // Ghi log cho audit trail
                await _auditLogService.LogActionAsync(
                    isSuccessful ? "LoginSuccess" : "LoginFailed",
                    "Authentication",
                    userId,
                    null,
                    JsonSerializer.Serialize(new
                    {
                        IPAddress = ipAddress,
                        Success = isSuccessful,
                        Notes = notes,
                        Timestamp = DateTime.Now
                    }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log login attempt");
            }
        }

        // Overload cho trường hợp không có userId (failed login)
        private async Task LogLoginAttempt(string username, string ipAddress, bool isSuccessful, string? notes = null)
        {
            try
            {
                await _auditLogService.LogActionAsync(
                    "LoginFailed",
                    "Authentication",
                    null,
                    null,
                    JsonSerializer.Serialize(new
                    {
                        Username = username,
                        IPAddress = ipAddress,
                        Success = isSuccessful,
                        Notes = notes,
                        Timestamp = DateTime.Now
                    }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log failed login attempt");
            }
        }

  



        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromQuery] string token)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u =>
                    u.EmailConfirmationToken == token &&
                    u.EmailConfirmationTokenExpiry > DateTime.Now);

                if (user == null)
                {
                    return BadRequest(new { message = "Token không hợp lệ hoặc đã hết hạn." });
                }

                user.IsEmailConfirmed = true;
                user.AccountStatus = "Active";
                user.EmailConfirmationToken = null;
                user.EmailConfirmationTokenExpiry = null;

                await _context.SaveChangesAsync();

                await _auditLogService.LogActionAsync(
                    "EmailConfirmed",
                    "Users",
                    user.Id,
                    null,
                    JsonSerializer.Serialize(new
                    {
                        ConfirmedAt = DateTime.Now
                    }));

                return Ok(new { message = "Email đã được xác nhận thành công. Tài khoản của bạn đã được kích hoạt." });
            }
            catch (Exception ex)
            {
                await _auditLogService.LogActionAsync(
                    "EmailConfirmationError",
                    "System",
                    null,
                    null,
                    JsonSerializer.Serialize(new
                    {
                        Error = ex.Message,
                        StackTrace = ex.StackTrace,
                        Token = token
                    }));
                return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
            }
        }
        [HttpPost("resend-confirmation-email")]
        public async Task<IActionResult> ResendConfirmationEmail([FromBody] ResendConfirmationRequest request)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
                if (user == null)
                {
                    return NotFound(new { message = "Không tìm thấy tài khoản với email này." });
                }

                if (user.IsEmailConfirmed)
                {
                    return BadRequest(new { message = "Email đã được xác nhận trước đó." });
                }

                // Tạo token mới
                user.EmailConfirmationToken = Guid.NewGuid().ToString();
                user.EmailConfirmationTokenExpiry = DateTime.Now.AddHours(24);
                await _context.SaveChangesAsync();

                // Gửi email
                await _emailService.SendEmailConfirmationAsync(
                    user.Email,
                    user.FullName ?? user.UserName,
                    user.EmailConfirmationToken);

                await _auditLogService.LogActionAsync(
                    "ResendConfirmationEmail",
                    "Users",
                    user.Id,
                    null,
                    JsonSerializer.Serialize(new
                    {
                        ResentAt = DateTime.Now
                    }));

                return Ok(new { message = "Đã gửi lại email xác nhận. Vui lòng kiểm tra hộp thư của bạn." });
            }
            catch (Exception ex)
            {
                await _auditLogService.LogActionAsync(
                    "ResendConfirmationError",
                    "System",
                    null,
                    null,
                    JsonSerializer.Serialize(new
                    {
                        Error = ex.Message,
                        StackTrace = ex.StackTrace,
                        Email = request.Email
                    }));
                return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
            }
        }

        public class ResendConfirmationRequest
        {
            public string Email { get; set; }
        }
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt", new CookieOptions
            {
                Path = "/",
                Secure = true,
                HttpOnly = true,
                SameSite = SameSiteMode.None
            });

            return Ok(new { message = "Đăng xuất thành công." });
        }
    }
}