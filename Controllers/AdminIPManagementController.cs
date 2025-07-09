using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using thuctap2025.Data;
using thuctap2025.DTOs;
using thuctap2025.Models;

namespace thuctap2025.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminIPManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminIPManagementController> _logger;

        public AdminIPManagementController(
            ApplicationDbContext context,
            ILogger<AdminIPManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Lấy danh sách tất cả IP bị ban
        [HttpGet("banned-ips")]
        public async Task<IActionResult> GetBannedIPs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null)
        {
            try
            {
                var query = _context.BannedIPs.AsQueryable();

                // Tìm kiếm theo IP hoặc lý do ban
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(b => b.IPAddress.Contains(search) ||
                                       (b.BanReason != null && b.BanReason.Contains(search)));
                }

                var totalCount = await query.CountAsync();

                var bannedIPs = await query
                    .OrderByDescending(b => b.BannedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(b => new BannedIPResponse
                    {
                        Id = b.Id,
                        IPAddress = b.IPAddress,
                        BanReason = b.BanReason,
                        BannedAt = b.BannedAt,
                        BannedBy = b.BannedBy
                    })
                    .ToListAsync();

                return Ok(new PagedResponse<BannedIPResponse>
                {
                    Data = bannedIPs,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting banned IPs list");
                return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
            }
        }

        // Ban một IP
        [HttpPost("ban-ip")]
        public async Task<IActionResult> BanIP([FromBody] BanIPRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.IPAddress))
                {
                    return BadRequest(new { message = "IP Address là bắt buộc." });
                }

                // Kiểm tra IP đã bị ban chưa
                var existingBan = await _context.BannedIPs
                    .FirstOrDefaultAsync(b => b.IPAddress == request.IPAddress);

                if (existingBan != null)
                {
                    return Conflict(new
                    {
                        message = "IP này đã bị ban trước đó.",
                        bannedAt = existingBan.BannedAt,
                        bannedBy = existingBan.BannedBy,
                        banReason = existingBan.BanReason
                    });
                }

                var bannedIP = new BannedIP
                {
                    IPAddress = request.IPAddress,
                    BanReason = request.BanReason,
                    BannedAt = DateTime.Now,
                    BannedBy = User.Identity?.Name ?? "System"
                };

                _context.BannedIPs.Add(bannedIP);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "IP đã được ban thành công.",
                    data = new BannedIPResponse
                    {
                        Id = bannedIP.Id,
                        IPAddress = bannedIP.IPAddress,
                        BanReason = bannedIP.BanReason,
                        BannedAt = bannedIP.BannedAt,
                        BannedBy = bannedIP.BannedBy
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error banning IP {IPAddress}", request.IPAddress);
                return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
            }
        }

    
        // Unban một IP
        [HttpDelete("unban-ip/{id}")]
        public async Task<IActionResult> UnbanIP(int id)
        {
            try
            {
                var bannedIP = await _context.BannedIPs.FindAsync(id);

                if (bannedIP == null)
                {
                    return NotFound(new { message = "Không tìm thấy IP trong danh sách ban." });
                }

                var ipAddress = bannedIP.IPAddress;

  
                var relatedLoginHistory = await _context.UserLoginHistories
                    .Where(x => x.IPAddress == ipAddress)
                    .ToListAsync();

                if (relatedLoginHistory.Any())
                {
                    _context.UserLoginHistories.RemoveRange(relatedLoginHistory);
                }

                _context.BannedIPs.Remove(bannedIP);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "IP đã được unban thành công và lịch sử đăng nhập liên quan đã được xóa.",
                    ipAddress = ipAddress,
                    deletedLoginHistoryCount = relatedLoginHistory.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unbanning IP with ID {Id}", id);
                return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
            }
        }

        // Unban IP theo địa chỉ IP
        [HttpPost("unban-ip-by-address")]
        public async Task<IActionResult> UnbanIPByAddress([FromBody] UnbanIPRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.IPAddress))
                {
                    return BadRequest(new { message = "IP Address là bắt buộc." });
                }

                var bannedIP = await _context.BannedIPs
                    .FirstOrDefaultAsync(b => b.IPAddress == request.IPAddress);

                if (bannedIP == null)
                {
                    return NotFound(new { message = "IP này không có trong danh sách ban." });
                }

                // Xóa tất cả lịch sử đăng nhập liên quan đến IP này
                var relatedLoginHistory = await _context.UserLoginHistories
                    .Where(x => x.IPAddress == request.IPAddress)
                    .ToListAsync();

                if (relatedLoginHistory.Any())
                {
                    _context.UserLoginHistories.RemoveRange(relatedLoginHistory);
                }

                _context.BannedIPs.Remove(bannedIP);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "IP đã được unban thành công và lịch sử đăng nhập liên quan đã được xóa.",
                    ipAddress = request.IPAddress,
                    deletedLoginHistoryCount = relatedLoginHistory.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unbanning IP {IPAddress}", request.IPAddress);
                return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
            }
        }

        // Lấy lịch sử đăng nhập của tất cả users
        [HttpGet("login-history")]
        public async Task<IActionResult> GetLoginHistory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? ipAddress = null,
            [FromQuery] int? userId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var query = _context.UserLoginHistories
                    .Include(h => h.User)
                    .AsQueryable();

                // Lọc theo IP
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    query = query.Where(h => h.IPAddress.Contains(ipAddress));
                }

                // Lọc theo User ID
                if (userId.HasValue)
                {
                    query = query.Where(h => h.UserId == userId.Value);
                }

                // Lọc theo khoảng thời gian
                if (fromDate.HasValue)
                {
                    query = query.Where(h => h.LoginTime >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(h => h.LoginTime <= toDate.Value);
                }

                var totalCount = await query.CountAsync();

                var loginHistory = await query
                    .OrderByDescending(h => h.LoginTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(h => new LoginHistoryResponse
                    {
                        Id = h.Id,
                        UserId = h.UserId,
                        Username = h.User.UserName,
                        FullName = h.User.FullName,
                        IPAddress = h.IPAddress,
                        LoginTime = h.LoginTime,
                        DeviceInfo = h.DeviceInfo
                    })
                    .ToListAsync();

                return Ok(new PagedResponse<LoginHistoryResponse>
                {
                    Data = loginHistory,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting login history");
                return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
            }
        }

        // Lấy lịch sử đăng nhập của một user cụ thể
        [HttpGet("login-history/user/{userId}")]
        public async Task<IActionResult> GetUserLoginHistory(
            int userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Kiểm tra user có tồn tại không
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "Không tìm thấy user." });
                }

                var query = _context.UserLoginHistories
                    .Where(h => h.UserId == userId);

                var totalCount = await query.CountAsync();

                var loginHistory = await query
                    .OrderByDescending(h => h.LoginTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(h => new LoginHistoryResponse
                    {
                        Id = h.Id,
                        UserId = h.UserId,
                        Username = user.UserName,
                        FullName = user.FullName,
                        IPAddress = h.IPAddress,
                        LoginTime = h.LoginTime,
                        DeviceInfo = h.DeviceInfo
                    })
                    .ToListAsync();

                return Ok(new PagedResponse<LoginHistoryResponse>
                {
                    Data = loginHistory,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user login history for user {UserId}", userId);
                return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
            }
        }

        // Thống kê IP đăng nhập
        [HttpGet("ip-statistics")]
        public async Task<IActionResult> GetIPStatistics(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int limit = 50)
        {
            try
            {
                var query = _context.UserLoginHistories.AsQueryable();

                // Lọc theo khoảng thời gian
                if (fromDate.HasValue)
                {
                    query = query.Where(h => h.LoginTime >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(h => h.LoginTime <= toDate.Value);
                }

                var ipStats = await query
                    .GroupBy(h => h.IPAddress)
                    .Select(g => new IPStatisticsResponse
                    {
                        IPAddress = g.Key,
                        LoginCount = g.Count(),
                        UniqueUsers = g.Select(h => h.UserId).Distinct().Count(),
                        LastLogin = g.Max(h => h.LoginTime),
                        FirstLogin = g.Min(h => h.LoginTime)
                    })
                    .OrderByDescending(s => s.LoginCount)
                    .Take(limit)
                    .ToListAsync();

                return Ok(ipStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting IP statistics");
                return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
            }
        }

        // Kiểm tra một IP có bị ban không
        [HttpGet("check-ip/{ipAddress}")]
        public async Task<IActionResult> CheckIPStatus(string ipAddress)
        {
            try
            {
                var bannedIP = await _context.BannedIPs
                    .FirstOrDefaultAsync(b => b.IPAddress == ipAddress);

                if (bannedIP != null)
                {
                    return Ok(new
                    {
                        isBanned = true,
                        banInfo = new BannedIPResponse
                        {
                            Id = bannedIP.Id,
                            IPAddress = bannedIP.IPAddress,
                            BanReason = bannedIP.BanReason,
                            BannedAt = bannedIP.BannedAt,
                            BannedBy = bannedIP.BannedBy
                        }
                    });
                }

                // Lấy thống kê về IP này
                var loginCount = await _context.UserLoginHistories
                    .CountAsync(h => h.IPAddress == ipAddress);

                var uniqueUsers = await _context.UserLoginHistories
                    .Where(h => h.IPAddress == ipAddress)
                    .Select(h => h.UserId)
                    .Distinct()
                    .CountAsync();

                var lastLogin = await _context.UserLoginHistories
                    .Where(h => h.IPAddress == ipAddress)
                    .MaxAsync(h => (DateTime?)h.LoginTime);

                return Ok(new
                {
                    isBanned = false,
                    statistics = new
                    {
                        ipAddress = ipAddress,
                        loginCount = loginCount,
                        uniqueUsers = uniqueUsers,
                        lastLogin = lastLogin
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking IP status for {IPAddress}", ipAddress);
                return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
            }
        }
    }

}