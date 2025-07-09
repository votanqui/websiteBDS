using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using thuctap2025.Data;
using System.Text.Json;
using thuctap2025.DTOs;
using thuctap2025.Models;

namespace thuctap2025.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;
        public AdminController(ApplicationDbContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }
        [HttpGet("property-views")]
        public async Task<IActionResult> GetPropertyViews(int? propertyId = null, int page = 1, int pageSize = 20)
        {
            var query = _context.PropertyViews
                .Include(v => v.Property)
                .Include(v => v.User)
                .AsQueryable();

            if (propertyId.HasValue)
            {
                query = query.Where(v => v.PropertyId == propertyId);
            }

            var totalCount = await query.CountAsync();

            var views = await query
                .OrderByDescending(v => v.ViewedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new
                {
                    v.Id,
                    v.PropertyId,
                    PropertyTitle = v.Property.Title,
                    UserId = v.UserId,
                    UserName = v.User != null ? v.User.FullName : null,
                    v.IPAddress,
                    v.UserAgent,
                    v.ViewedAt
                })
                .ToListAsync();

            return Ok(new
            {
                Total = totalCount,
                Page = page,
                PageSize = pageSize,
                Views = views
            });
        }
        [HttpGet("user-profile-views")]
        public async Task<IActionResult> GetUserProfileViews(int? viewedUserId = null, int page = 1, int pageSize = 20)
        {
            var query = _context.UserProfileViews
                .Include(v => v.ViewedUser)
                .Include(v => v.ViewerUser)
                .AsQueryable();

            if (viewedUserId.HasValue)
            {
                query = query.Where(v => v.ViewedUserId == viewedUserId);
            }

            var totalCount = await query.CountAsync();

            var views = await query
                .OrderByDescending(v => v.ViewedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new
                {
                    v.Id,
                    v.ViewedUserId,
                    ViewedUserName = v.ViewedUser.FullName,
                    ViewerUserId = v.ViewerUserId,
                    ViewerUserName = v.ViewerUser != null ? v.ViewerUser.FullName : null,
                    v.IPAddress,
                    v.UserAgent,
                    v.ViewedAt
                })
                .ToListAsync();

            return Ok(new
            {
                Total = totalCount,
                Page = page,
                PageSize = pageSize,
                Views = views
            });
        }
        [HttpGet("news-views")]
        public async Task<IActionResult> GetNewsViews(int? newsId = null, int page = 1, int pageSize = 20)
        {
            var query = _context.NewsViews
                .Include(v => v.News)
                .Include(v => v.User)
                .AsQueryable();

            if (newsId.HasValue)
            {
                query = query.Where(v => v.NewsId == newsId);
            }

            var totalCount = await query.CountAsync();

            var views = await query
                .OrderByDescending(v => v.ViewedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new
                {
                    v.Id,
                    v.NewsId,
                    NewsTitle = v.News.Title,
                    UserId = v.UserId,
                    UserName = v.User != null ? v.User.FullName : null,
                    v.IPAddress,
                    v.UserAgent,
                    v.ViewedAt
                })
                .ToListAsync();

            return Ok(new
            {
                Total = totalCount,
                Page = page,
                PageSize = pageSize,
                Views = views
            });
        }

        [HttpPost("add-amount")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddAmountToUser([FromBody] AddAmountRequest request)
        {
            var user = await _context.Users.FindAsync(request.UserId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            var oldUser = new Users
            {
                Id = user.Id,
                UserName = user.UserName,
                Amount = user.Amount
            };

            user.Amount += request.Amount;

            var newUser = new Users
            {
                Id = user.Id,
                UserName = user.UserName,
                Amount = user.Amount
            };

            await _context.SaveChangesAsync();

            await _auditLogService.LogActionAsync(
                "AddAmount",
                 "Users",
                 user.Id,
                JsonSerializer.Serialize(oldUser),
                JsonSerializer.Serialize(newUser)
            );

            return Ok(new { message = "Amount added successfully", newAmount = user.Amount });
        }

        [HttpGet("allproperties")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetAllPropertiesForAdmin(
             int page = 1,
             int pageSize = 10,
             string search = "",
             string status = "",
             bool? isVip = null // ✅ Thêm bộ lọc tin VIP
         )
        {
            var query = _context.Properties
                .Include(p => p.PropertyImages)
                .Include(p => p.PropertyCategoryMappings).ThenInclude(m => m.Category)
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt)
                .AsQueryable();

            // Tìm kiếm theo từ khóa
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p =>
                    p.Title.Contains(search) ||
                    (p.User != null && p.User.FullName.Contains(search)) ||
                    (p.Address != null && p.Address.Contains(search)) ||
                    p.PropertyCategoryMappings.Any(m => m.Category.Name.Contains(search))
                );
            }

            // Tìm kiếm theo status
            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.Status == status);
            }

            // ✅ Lọc tin VIP
            if (isVip.HasValue)
            {
                query = query.Where(p => p.IsVip == isVip.Value);
            }

            var totalItems = query.Count();

            var propertyList = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var propertyIds = propertyList.Select(p => p.Id).ToList();

            var reportCounts = _context.ReportPosts
                .Where(r => propertyIds.Contains(r.PropertyId))
                .GroupBy(r => r.PropertyId)
                .Select(g => new { PropertyId = g.Key, Count = g.Count() })
                .ToDictionary(g => g.PropertyId, g => g.Count);

            var viewCounts = _context.PropertyViews
                .Where(v => propertyIds.Contains(v.PropertyId))
                .GroupBy(v => v.PropertyId)
                .Select(g => new { PropertyId = g.Key, Count = g.Count() })
                .ToDictionary(g => g.PropertyId, g => g.Count);

            var result = propertyList.Select(p => new
            {
                Id = p.Id,
                Title = p.Title,
                User = p.User != null ? p.User.FullName : "Unknown",
                Price = p.Price,
                Location = p.Address ?? "Unknown",
                Beds = p.Bedrooms,
                Baths = p.Bathrooms,
                Area = p.Area,
                Status = p.Status,
                IsVip = p.IsVip, // ✅ Trả về tin VIP
                CreatedAt = p.CreatedAt,
                Image = p.PropertyImages
                    .Where(img => img.IsPrimary)
                    .OrderBy(img => img.SortOrder)
                    .Select(img => img.ImageUrl)
                    .FirstOrDefault() ?? "default.jpg",
                Categories = p.PropertyCategoryMappings.Select(m => m.Category.Name).ToList(),
                ReportCount = reportCounts.ContainsKey(p.Id) ? reportCounts[p.Id] : 0,
                ViewCount = viewCounts.ContainsKey(p.Id) ? viewCounts[p.Id] : 0,
            });

            return Ok(new
            {
                data = result,
                totalItems,
                currentPage = page,
                pageSize
            });
        }

        [HttpGet("getuser")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<object>> GetUsers(
            int page = 1,
            int pageSize = 10,
            string search = "",
            string accountStatus = "",
            string role = ""
        )
        {
            var query = _context.Users
                .Include(u => u.Properties)
                    .ThenInclude(p => p.PropertyImages)
                .AsQueryable();

            // Tìm kiếm theo FullName, Email, UserName
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u =>
                    u.FullName.Contains(search) ||
                    u.Email.Contains(search) ||
                    u.UserName.Contains(search)
                );
            }

            // Lọc theo AccountStatus (nếu có)
            if (!string.IsNullOrEmpty(accountStatus))
            {
                query = query.Where(u => u.AccountStatus == accountStatus);
            }

            // Lọc theo Role (nếu có)
            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.Role == role);
            }

            var totalItems = await query.CountAsync();

            var userList = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.FullName,
                    u.Email,
                    u.PhoneNumber,
                    u.AccountStatus,
                    u.Role,
                    u.Amount,
                    u.CreatedAt,
                    u.LastLogin,
                    Properties = u.Properties.Select(p => new
                    {
                        p.Id,
                        p.Title,
                        p.Description,
                        p.Price,
                        p.Address,
                        p.CreatedAt,
                        Image = p.PropertyImages
                            .Where(img => img.IsPrimary)
                            .OrderBy(img => img.SortOrder)
                            .Select(img => img.ImageUrl)
                            .FirstOrDefault() ?? "default.jpg"
                    })
                })
                .ToListAsync();

            return Ok(new
            {
                data = userList,
                totalItems,
                currentPage = page,
                pageSize
            });
        }

        [HttpPut("status-user/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] string newStatus)
        {
            var allowedStatuses = new List<string> { "Active", "Suspended", "Banned", "Pending" };

            // Kiểm tra nếu giá trị không hợp lệ
            if (!allowedStatuses.Contains(newStatus))
            {
                return BadRequest(new { message = "Trạng thái không hợp lệ. Chỉ được phép 'Active', 'Suspended' hoặc 'Banned'." });
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy người dùng." });
            }

            // Không cho phép thay đổi trạng thái của tài khoản Admin
            if (user.Role == "Admin")
            {
                return BadRequest(new { message = "Không thể thay đổi trạng thái của tài khoản Quản trị viên." });
            }

            user.AccountStatus = newStatus;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Trạng thái tài khoản đã được cập nhật thành công." });
        }


        public class UpdateStatusDto
        {
            public string NewStatus { get; set; }
        }

        [HttpPut("updatestatus/{propertyId}")]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdatePropertyStatus(int propertyId, [FromBody] UpdateStatusDto dto)
        {
            var property = _context.Properties.FirstOrDefault(p => p.Id == propertyId);

            if (property == null)
                return NotFound(new { message = "Không tìm thấy bài đăng." });

            var allowedStatuses = new[] { "Pending", "Approved", "Rejected", "Sold", "Pinned" };
            if (!allowedStatuses.Contains(dto.NewStatus))
                return BadRequest(new { message = "Trạng thái không hợp lệ." });

            property.Status = dto.NewStatus;
            property.UpdatedAt = DateTime.Now;

            _context.SaveChanges();

            return Ok(new { message = "Cập nhật trạng thái thành công." });
        }


        [HttpGet("AuditLog")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? actionType = null,
            [FromQuery] string? tableName = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.AuditLogs.AsQueryable();


                if (!string.IsNullOrWhiteSpace(actionType))
                    query = query.Where(x => x.ActionType == actionType);

                if (!string.IsNullOrWhiteSpace(tableName))
                    query = query.Where(x => x.TableName == tableName);


                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var lowerSearch = searchTerm.ToLower();
                    query = query.Where(x =>
                        (!string.IsNullOrEmpty(x.ActionType) && x.ActionType.ToLower().Contains(lowerSearch)) ||
                        (!string.IsNullOrEmpty(x.TableName) && x.TableName.ToLower().Contains(lowerSearch)) ||
                        (x.UserId != null && _context.Users
                            .Where(u => u.Id == x.UserId)
                            .Any(u => u.UserName.ToLower().Contains(lowerSearch)))
                    );
                }

                var totalCount = await query.CountAsync();

                var logsRaw = await query
                    .OrderByDescending(x => x.ActionTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var userIds = logsRaw
                    .Where(x => x.UserId.HasValue)
                    .Select(x => x.UserId.Value)
                    .Distinct()
                    .ToList();

                var usersDict = await _context.Users
                    .Where(u => userIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.UserName);

                var logs = logsRaw.Select(x => new
                {
                    x.Id,
                    x.ActionType,
                    x.TableName,
                    x.RecordId,
                    x.UserId,
                    Username = x.UserId.HasValue && usersDict.ContainsKey(x.UserId.Value) ? usersDict[x.UserId.Value] : null,
                    x.IPAddress,
                    x.UserAgent,
                    ActionTime = x.ActionTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    OldValues = x.OldValues != null ? JsonSerializer.Deserialize<object>(x.OldValues) : null,
                    NewValues = x.NewValues != null ? JsonSerializer.Deserialize<object>(x.NewValues) : null
                });

                return Ok(new
                {
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    Data = logs
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "Lỗi khi lấy dữ liệu audit log",
                    Error = ex.Message
                });
            }
        }


        [HttpGet("GetReportPosts")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetReportPosts(
    [FromQuery] string? search = null,
    [FromQuery] string? status = null,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.ReportPosts
                    .Include(r => r.User)
                    .Include(r => r.Property)
                    .AsQueryable();

                // Tìm kiếm theo username, property title, hoặc reason
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(r =>
                        r.User.UserName.Contains(search) ||
                        r.Property.Title.Contains(search) ||
                        r.Reason.Contains(search)
                    );
                }

                // Lọc theo status nếu có
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(r => r.Status == status);
                }

                var totalCount = await query.CountAsync();

                var reports = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new
                    {
                        r.Id,
                        r.PropertyId,
                        PropertyTitle = r.Property.Title,
                        r.UserId,
                        Username = r.User.UserName,
                        r.Reason,
                        r.Note,
                        r.Status,
                        CreatedAt = r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
                    })
                    .ToListAsync();

                return Ok(new
                {
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    Data = reports
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "Lỗi khi lấy danh sách báo cáo",
                    Error = ex.Message
                });
            }
        }

        [HttpPut("UpdateReportStatus/{id}")]
        public async Task<IActionResult> UpdateReportStatus(int id, [FromBody] UpdateReportStatusDto dto)
        {
            if (string.IsNullOrEmpty(dto.Status))
            {
                return BadRequest("Status is required.");
            }

            var report = await _context.ReportPosts.FindAsync(id);

            if (report == null)
            {
                return NotFound("Report not found.");
            }

            report.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Status updated successfully" });
        }
    }
}
