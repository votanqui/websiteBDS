using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using thuctap2025.Data;
using thuctap2025.DTOs;

namespace thuctap2025.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        public class UploadAvatarDto
        {
            [Required]
            public IFormFile File { get; set; } = default!;
        }

        [HttpPost("UploadAvatar")]
        [Authorize]
        public async Task<IActionResult> UploadAvatar([FromForm] UploadAvatarDto dto)
        {
            var file = dto.File;

            if (file == null || file.Length == 0)
                return BadRequest(new
                {
                    success = false,
                    message = "Không có tệp nào được tải lên."
                });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "Không xác định được người dùng."
                });
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy người dùng."
                });
            }

            // Lưu đường dẫn ảnh cũ để xóa sau
            string oldAvatarUrl = user.AvatarUrl;

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images-profile");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi lưu tệp: " + ex.Message
                });
            }

            var imageUrl = $"/images-profile/{uniqueFileName}";

            // Cập nhật đường dẫn ảnh mới trong database
            user.AvatarUrl = imageUrl;
            await _context.SaveChangesAsync();

            // Xóa ảnh cũ sau khi cập nhật thành công
            if (!string.IsNullOrEmpty(oldAvatarUrl))
            {
                try
                {
                    var oldFileNameOnly = oldAvatarUrl.Replace("/images-profile/", "").TrimStart('/');
                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images-profile", oldFileNameOnly);
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }
                catch (Exception ex)
                {
                    // Log lỗi nhưng không trả về lỗi vì ảnh mới đã upload thành công
                    Console.WriteLine($"Không thể xóa ảnh cũ: {ex.Message}");
                }
            }

            return Ok(new
            {
                success = true,
                message = "Tải ảnh đại diện thành công.",
                imageUrl = imageUrl
            });
        }

        [HttpDelete("RemoveAvatar")]
        [Authorize]
        public async Task<IActionResult> RemoveAvatar()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.AvatarUrl))
                return NotFound("Không tìm thấy ảnh đại diện.");

            // Xóa file ảnh vật lý
            var fileNameOnly = user.AvatarUrl.Replace("/images-profile/", "").TrimStart('/');
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images-profile", fileNameOnly);
            if (System.IO.File.Exists(imagePath))
                System.IO.File.Delete(imagePath);

            // Xóa đường dẫn avatar khỏi DB
            user.AvatarUrl = null;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Đã xoá ảnh đại diện thành công." });
        }
        [HttpGet("UsersWithProperties/{userId}")]
        public async Task<IActionResult> GetUserProfileWithProperties(
      int userId,
      int page = 1,
      int pageSize = 10,
      string? sortField = "createdAt",
      string? sortOrder = "desc", // "asc" or "desc"
      decimal? minPrice = null,
      decimal? maxPrice = null,
      double? minArea = null,
      double? maxArea = null,
      string? status = null
  )
        {
            // Lấy thông tin user + Properties + Categories + Images
            var user = await _context.Users
                .Include(u => u.Properties)
                    .ThenInclude(p => p.PropertyImages)
                .Include(u => u.Properties)
                    .ThenInclude(p => p.PropertyCategoryMappings)
                        .ThenInclude(pcm => pcm.Category)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }
            var profileViewCount = await _context.UserProfileViews
    .CountAsync(v => v.ViewedUserId == userId);
            // Lọc properties
            var query = user.Properties.AsQueryable();

            if (minPrice.HasValue)
                query = query.Where(p => p.Price.HasValue && p.Price >= minPrice);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price.HasValue && p.Price <= maxPrice);

            if (minArea.HasValue)
                query = query.Where(p => p.Area.HasValue && p.Area >= minArea);

            if (maxArea.HasValue)
                query = query.Where(p => p.Area.HasValue && p.Area <= maxArea);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            // Lấy PropertyId
            var propertyIds = query.Select(p => p.Id).ToList();

            // View count (truy vấn 1 lần)
            var viewCounts = await _context.PropertyViews
                .Where(v => propertyIds.Contains(v.PropertyId))
                .GroupBy(v => v.PropertyId)
                .Select(g => new { PropertyId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.PropertyId, g => g.Count);

            // Sorting
            switch (sortField?.ToLower())
            {
                case "price":
                    query = (sortOrder == "asc") ? query.OrderBy(p => p.Price) : query.OrderByDescending(p => p.Price);
                    break;
                case "area":
                    query = (sortOrder == "asc") ? query.OrderBy(p => p.Area) : query.OrderByDescending(p => p.Area);
                    break;
                case "isvip":
                    query = (sortOrder == "asc") ? query.OrderBy(p => p.IsVip) : query.OrderByDescending(p => p.IsVip);
                    break;
                case "createdat":
                default:
                    query = (sortOrder == "asc") ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt);
                    break;
            }

            // Pagination
            var totalProperties = query.Count();
            var skip = (page - 1) * pageSize;

            var pagedProperties = query
                .Skip(skip)
                .Take(pageSize)
                .Select(p => new PropertySummaryDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Price = p.Price,
                    Area = p.Area,
                    Address = p.Address,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt,
                    IsVip = p.IsVip,
                    Bedrooms = p.Bedrooms,           // 🛏️ thêm số phòng ngủ
                    Bathrooms = p.Bathrooms,
                    MainImageUrl = p.PropertyImages
                                    .OrderByDescending(pi => pi.IsPrimary)
                                    .ThenBy(pi => pi.SortOrder)
                                    .Select(pi => pi.ImageUrl)
                                    .FirstOrDefault(),

                    Categories = p.PropertyCategoryMappings
                        .Select(pcm => pcm.Category.Name)
                        .ToList(),
                    ViewCount = viewCounts.ContainsKey(p.Id) ? viewCounts[p.Id] : 0
                })
                .ToList();

            // Build response DTO
            var response = new UserProfileWithPropertiesResponse
            {
                UserInfo = new UserProfileDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    AvatarUrl = user.AvatarUrl,
                    PhoneNumber = user.PhoneNumber,
                    Email = user.Email,
                    JoinDate = user.CreatedAt,
                    TotalProperties = totalProperties,
                    ProfileViewCount = profileViewCount

                },
                Properties = pagedProperties,
                Pagination = new PaginationDto
                {
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalProperties = totalProperties,
                    TotalPages = (int)Math.Ceiling(totalProperties / (double)pageSize)
                }
            };

            return Ok(response);
        }



        [HttpGet("GetProfile")]
        public async Task<IActionResult> LayProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { success = false, message = "Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn." });

            var user = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new {
                    u.UserName,
                    u.FullName,
                    u.Email,
                    u.PhoneNumber,
                    u.AvatarUrl,
                    u.AccountStatus,
                    u.Role,
                    u.Amount,
                    u.CreatedAt,
                    u.LastLogin
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { success = false, message = "Không tìm thấy thông tin người dùng." });

            return Ok(new { success = true, data = user });
        }

        [HttpPut("UpdateProfile")]
        public async Task<IActionResult> CapNhatProfile([FromBody] UserProfileUpdateDto dto)
        {
            if (dto == null)
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized(new { success = false, message = "Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn." });

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound(new { success = false, message = "Người dùng không tồn tại." });

            user.FullName = dto.FullName ?? user.FullName;
            user.Email = dto.Email ?? user.Email;
            user.PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber;
            user.AvatarUrl = dto.AvatarUrl ?? user.AvatarUrl;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                user.PasswordHash = dto.Password;
            }
            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { success = true, message = "Cập nhật thông tin thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi máy chủ, vui lòng thử lại sau.", detail = ex.Message });
            }
        }
    }
}