using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using thuctap2025.Data;
using thuctap2025.Models;
using System.Text.Json;
using thuctap2025.DTOs;

namespace thuctap2025.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;

        public UserController(ApplicationDbContext context, AuthService authService, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }

        [Authorize]
        [HttpPost("upgrade-vip")]
        public async Task<IActionResult> UpgradeToVip([FromBody] UpgradeVipRequest request)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            var userIdClaim = identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
            {
                return Unauthorized(new { message = "Không xác định được người dùng." });
            }

            var property = await _context.Properties
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == request.PropertyId);

            if (property == null)
                return NotFound(new { message = "Không tìm thấy bất động sản." });

            if (property.UserId != currentUserId)
                return Forbid("Bạn không có quyền nâng cấp tin này.");

            if (property.IsVip)
                return BadRequest(new { message = "Tin đã là VIP rồi." });

            const decimal vipCost = 10000m;
            var user = property.User;

            if (user.Amount < vipCost)
                return BadRequest(new { message = "Số dư không đủ để nâng cấp VIP." });

            // ✅ Clone dữ liệu cũ để ghi log
            var oldProperty = new
            {
                property.IsVip,
                property.VipStartDate,
                property.VipEndDate,
                user.Amount
            };

            // ✅ Thay đổi dữ liệu
            user.Amount -= vipCost;
            property.IsVip = true;
            property.VipStartDate = DateTime.Now;
            property.VipEndDate = DateTime.Now.AddDays(7);

            await _context.SaveChangesAsync();

            // ✅ Clone dữ liệu mới để ghi log
            var newProperty = new
            {
                property.IsVip,
                property.VipStartDate,
                property.VipEndDate,
                user.Amount
            };

            // ✅ Ghi audit log
            await _auditLogService.LogActionAsync(
                "UpgradeToVip",
                "Properties",
                property.Id,
                JsonSerializer.Serialize(oldProperty),
                JsonSerializer.Serialize(newProperty)
            );

            return Ok(new
            {
                message = "Nâng cấp VIP thành công.",
                newBalance = user.Amount,
                vipStart = property.VipStartDate,
                vipEnd = property.VipEndDate
            });
        }

        [HttpPost("CreateProperty")]
        [Authorize]
        public async Task<IActionResult> CreateProperty([FromBody] CreatePropertyRequest request)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            var userIdClaim = identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "Không thể xác thực người dùng." });
            }

            int userId = int.Parse(userIdClaim);

            // ✅ Kiểm tra trạng thái tài khoản
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return Unauthorized(new { message = "Người dùng không tồn tại." });
            }

            if (user.AccountStatus == "Pending")
            {
                return StatusCode(403, new { message = "Tài khoản của bạn đang chờ duyệt. Vui lòng thử lại sau khi tài khoản được phê duyệt." });
            }
            var property = new Property
            {
                Title = request.Title,
                Description = request.Description,
                Address = request.Address,
                Price = request.Price,
                Area = request.Area,
                Bedrooms = request.Bedrooms,
                Bathrooms = request.Bathrooms,
                UserId = userId,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                CreatedAt = DateTime.Now
            };

            _context.Properties.Add(property);
            await _context.SaveChangesAsync();

            // Gán danh mục
            foreach (var categoryId in request.CategoryIds)
            {
                _context.PropertyCategoryMappings.Add(new PropertyCategoryMapping
                {
                    PropertyId = property.Id,
                    CategoryId = categoryId
                });
            }

            // Gán đặc điểm
            foreach (var feature in request.Features)
            {
                _context.PropertyFeatures.Add(new PropertyFeature
                {
                    PropertyId = property.Id,
                    FeatureName = feature.FeatureName,
                    FeatureValue = feature.FeatureValue
                });
            }

            // Gán ảnh
            int index = 0;
            bool isPrimarySet = false;

            foreach (var imageUrl in request.ImageUrls)
            {
                bool isPrimary = false;

                if (!isPrimarySet && imageUrl.Trim() == request.MainImage.Trim())
                {
                    isPrimary = true;
                    isPrimarySet = true;
                }

                _context.PropertyImages.Add(new PropertyImage
                {
                    PropertyId = property.Id,
                    ImageUrl = imageUrl,
                    IsPrimary = isPrimary,
                    SortOrder = index++
                });
            }

            // Thêm SEO info
            _context.SeoInfos.Add(new SeoInfo
            {
                PropertyId = property.Id,
                MetaTitle = request.MetaTitle,
                MetaDescription = request.MetaDescription,
                MetaKeywords = request.MetaKeywords,
            });

            await _context.SaveChangesAsync();

            // Tạo object mới để ghi log chứa tất cả thông tin
            var propertyForLog = new
            {
                property.Id,
                property.Title,
                property.Description,
                property.Address,
                property.Price,
                property.Area,
                property.Bedrooms,
                property.Bathrooms,
                property.Latitude,
                property.Longitude,
                property.CreatedAt,
                CategoryIds = request.CategoryIds,
                Features = request.Features,
                ImageUrls = request.ImageUrls,
                MainImage = request.MainImage,
                MetaTitle = request.MetaTitle,
                MetaDescription = request.MetaDescription,
                MetaKeywords = request.MetaKeywords,
            };
            // Ghi log với dữ liệu đã được format đúng
            await _auditLogService.LogActionAsync(
               "Create",
               "Properties",
               property.Id,
                null,
                JsonSerializer.Serialize(propertyForLog)
            );

            return Ok(new { message = "Property created successfully", id = property.Id });
        }
        [HttpGet("GetPropertyById/{id}")]
        public async Task<IActionResult> GetPropertyById(int id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Forbid();
            }
            var property = await _context.Properties
                .Where(p => p.Id == id)
               .Select(p => new CreatePropertyRequest
               {
                   Title = p.Title,
                   Description = p.Description,
                   Address = p.Address,
                   Price = p.Price ?? 0, // decimal?
                   Area = (float)(p.Area ?? 0), // double? -> float
                   Bedrooms = p.Bedrooms ?? 0,
                   Bathrooms = p.Bathrooms ?? 0,
                   UserId = p.UserId ,
                   Latitude = (float)(p.Latitude ?? 0), // double? -> float
                   Longitude = (float)(p.Longitude ?? 0), // double? -> float

                   CategoryIds = _context.PropertyCategoryMappings
                          .Where(c => c.PropertyId == id)
                          .Select(c => c.CategoryId)
                          .ToList(),

                   Features = _context.PropertyFeatures
                       .Where(f => f.PropertyId == id)
                       .Select(f => new FeatureDto
                       {
                           FeatureName = f.FeatureName,
                           FeatureValue = f.FeatureValue
                       }).ToList(),

                   ImageUrls = _context.PropertyImages
                        .Where(i => i.PropertyId == id)
                        .OrderBy(i => i.SortOrder)
                        .Select(i => i.ImageUrl)
                        .ToList(),

                   MainImage = _context.PropertyImages
                        .Where(i => i.PropertyId == id && i.IsPrimary)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault(),

                   MetaTitle = _context.SeoInfos.Where(s => s.PropertyId == id).Select(s => s.MetaTitle).FirstOrDefault(),
                   MetaDescription = _context.SeoInfos.Where(s => s.PropertyId == id).Select(s => s.MetaDescription).FirstOrDefault(),
                   MetaKeywords = _context.SeoInfos.Where(s => s.PropertyId == id).Select(s => s.MetaKeywords).FirstOrDefault(),
                   CanonicalUrl = _context.SeoInfos.Where(s => s.PropertyId == id).Select(s => s.CanonicalUrl).FirstOrDefault()
               })
                .FirstOrDefaultAsync();

            if (property == null)
                return NotFound();

            return Ok(property);
        }
        [HttpPut("EditProperty/{id}")]
        [Authorize]
        public async Task<IActionResult> EditProperty(int id, [FromBody] CreatePropertyRequest request)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property == null)
                return NotFound();

            var identity = HttpContext.User.Identity as ClaimsIdentity;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (property.UserId.ToString() != userIdClaim && userRole != "Admin")
            {
                return StatusCode(403, new { message = "Bạn không có quyền sửa bất động sản này." });
            }

            // Lưu giá trị cũ trước khi cập nhật
            var oldProperty = new
            {
                property.Title,
                property.Description,
                property.Address,
                property.Price,
                property.Area,
                property.Bedrooms,
                property.Bathrooms,
                property.Latitude,
                property.Longitude,
                Categories = await _context.PropertyCategoryMappings
                    .Where(c => c.PropertyId == id)
                    .Select(c => c.CategoryId)
                    .ToListAsync(),
                Features = await _context.PropertyFeatures
                    .Where(f => f.PropertyId == id)
                    .Select(f => new { f.FeatureName, f.FeatureValue })
                    .ToListAsync(),
                Images = await _context.PropertyImages
                    .Where(i => i.PropertyId == id)
                    .Select(i => new { i.ImageUrl, i.IsPrimary, i.SortOrder })
                    .ToListAsync(),
                SeoInfo = await _context.SeoInfos
                    .Where(s => s.PropertyId == id)
                    .Select(s => new { s.MetaTitle, s.MetaDescription, s.MetaKeywords, s.CanonicalUrl })
                    .FirstOrDefaultAsync()
            };

            // Cập nhật thông tin chính
            property.Title = request.Title;
            property.Description = request.Description;
            property.Address = request.Address;
            property.Price = request.Price;
            property.Area = request.Area;
            property.Bedrooms = request.Bedrooms;
            property.Bathrooms = request.Bathrooms;
            property.Latitude = request.Latitude;
            property.Longitude = request.Longitude;

            // Xóa cũ, thêm mới: category
            var oldCategories = _context.PropertyCategoryMappings.Where(c => c.PropertyId == id);
            _context.PropertyCategoryMappings.RemoveRange(oldCategories);
            foreach (var categoryId in request.CategoryIds)
            {
                _context.PropertyCategoryMappings.Add(new PropertyCategoryMapping
                {
                    PropertyId = id,
                    CategoryId = categoryId
                });
            }

            // Xóa cũ, thêm mới: features
            var oldFeatures = _context.PropertyFeatures.Where(f => f.PropertyId == id);
            _context.PropertyFeatures.RemoveRange(oldFeatures);
            foreach (var feature in request.Features)
            {
                _context.PropertyFeatures.Add(new PropertyFeature
                {
                    PropertyId = id,
                    FeatureName = feature.FeatureName,
                    FeatureValue = feature.FeatureValue
                });
            }

            // Xóa cũ, thêm mới: images
            var oldImages = _context.PropertyImages.Where(i => i.PropertyId == id);
            _context.PropertyImages.RemoveRange(oldImages);
            int index = 0;
            bool isPrimarySet = false;
            foreach (var imageUrl in request.ImageUrls)
            {
                bool isPrimary = false;
                if (!isPrimarySet && imageUrl.Trim() == request.MainImage.Trim())
                {
                    isPrimary = true;
                    isPrimarySet = true;
                }

                _context.PropertyImages.Add(new PropertyImage
                {
                    PropertyId = id,
                    ImageUrl = imageUrl,
                    IsPrimary = isPrimary,
                    SortOrder = index++
                });
            }

            // Cập nhật SEO info
            var seo = await _context.SeoInfos.FirstOrDefaultAsync(s => s.PropertyId == id);
            if (seo != null)
            {
                seo.MetaTitle = request.MetaTitle;
                seo.MetaDescription = request.MetaDescription;
                seo.MetaKeywords = request.MetaKeywords;
                seo.CanonicalUrl = request.CanonicalUrl;
            }
            else
            {
                _context.SeoInfos.Add(new SeoInfo
                {
                    PropertyId = id,
                    MetaTitle = request.MetaTitle,
                    MetaDescription = request.MetaDescription,
                    MetaKeywords = request.MetaKeywords,
                    CanonicalUrl = request.CanonicalUrl
                });
            }

            // Lưu giá trị mới sau khi cập nhật
            var newProperty = new
            {
                request.Title,
                request.Description,
                request.Address,
                request.Price,
                request.Area,
                request.Bedrooms,
                request.Bathrooms,
                request.Latitude,
                request.Longitude,
                Categories = request.CategoryIds,
                Features = request.Features,
                Images = request.ImageUrls.Select((url, i) => new
                {
                    ImageUrl = url,
                    IsPrimary = url.Trim() == request.MainImage.Trim() && i == request.ImageUrls.IndexOf(url),
                    SortOrder = i
                }),
                SeoInfo = new
                {
                    request.MetaTitle,
                    request.MetaDescription,
                    request.MetaKeywords,
                    request.CanonicalUrl
                }
            };

            // Ghi log
            await _auditLogService.LogActionAsync(
                "Update",
                "Properties",
                id,
                JsonSerializer.Serialize(oldProperty),
                JsonSerializer.Serialize(newProperty));

            await _context.SaveChangesAsync();
            return Ok(new { message = "Property updated successfully" });
        }


        [HttpGet("GetUserProperties/{userId}")]
        [Authorize]
        public IActionResult GetUserProperties(int userId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Kiểm tra xem userId trong URL có trùng với userId của người dùng hiện tại không
            if (userId.ToString() != userIdClaim)
            {
              
                return StatusCode(403, new { message = "Bạn không có quyền truy cập tài sản của người dùng khác." });
            }

            var properties = _context.Properties
                .Include(p => p.PropertyImages)
                .Include(p => p.PropertyCategoryMappings).ThenInclude(m => m.Category)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .ToList();

            var result = properties.Select(p => new
            {
                Id = p.Id,
                Title = p.Title,
                Price = p.Price,
                Location = p.Address ?? "Chưa rõ",
                Beds = p.Bedrooms,
                Baths = p.Bathrooms,
                Area = p.Area,
                IsVip = p.IsVip,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                Image = p.PropertyImages
                            .Where(img => img.IsPrimary)
                            .OrderBy(img => img.SortOrder)
                            .Select(img => img.ImageUrl)
                            .FirstOrDefault() ?? "default.jpg",
                Categories = p.PropertyCategoryMappings.Select(m => m.Category.Name).ToList()
            });

            return Ok(result);
        }
        [HttpPost("UploadImages")]
        [Authorize]
        public async Task<IActionResult> UploadImages([FromForm] List<IFormFile> files)

        {
            if (files == null || files.Count == 0)
                return BadRequest("No files uploaded");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uploadedUrls = new List<string>();

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                // Generate unique filename
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                uploadedUrls.Add($"/images/{uniqueFileName}");
            }

            return Ok(uploadedUrls);
        }
        [Authorize]
        [HttpDelete("RemoveImage")]
        public async Task<IActionResult> RemoveImage([FromQuery] string imageName)
        {
            if (string.IsNullOrEmpty(imageName))
                return BadRequest("Tên ảnh không được để trống.");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            int userId = int.Parse(userIdClaim);

            // So sánh đúng ImageUrl trong DB
            var image = await _context.PropertyImages
                .Include(pi => pi.Property)
                .FirstOrDefaultAsync(pi => pi.ImageUrl == imageName);

            if (image == null)
                return NotFound("Ảnh không tồn tại.");

            if (image.Property.UserId != userId && userRole != "Admin")
            return StatusCode(403, new { message = "Bạn không có quyền xóa ảnh này." });
            // Cắt /images/ để xóa file
            var fileNameOnly = imageName.Replace("/images/", "").TrimStart('/');
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileNameOnly);
            if (System.IO.File.Exists(imagePath))
                System.IO.File.Delete(imagePath);

            _context.PropertyImages.Remove(image);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa ảnh thành công." });
        }


        [HttpGet("categories")]
        public IActionResult GetAllCategories()
        {
            var categories = _context.PropertyCategories
                .Select(c => new PropertyCategory
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    CreatedAt =c.CreatedAt,
                })
                .ToList();

            return Ok(categories);
        }
        [HttpGet("profile")]
        [Authorize]
        public IActionResult GetProfile()
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            if (identity == null)
            {
                return Unauthorized(new { message = "Invalid token" });
            }
            var userIdClaim = identity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "Invalid user ID" });
            }
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }
            var profile = new
            {
                FullName = identity.FindFirst("FullName")?.Value,
                Role = identity.FindFirst(ClaimTypes.Role)?.Value,
                UserId = userId,
                AccountStatus = user.AccountStatus 
            };
            return Ok(profile);
        }

        [Authorize]
        [HttpDelete("DeleteProperty/{propertyId}")]
        public async Task<IActionResult> DeleteProperty(int propertyId)
        {
            // Lấy userId từ token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userIdClaim == null)
                return Unauthorized("Không tìm thấy user trong token.");

            int userId = int.Parse(userIdClaim);

            // Tìm bất động sản theo Id và include các thông tin liên quan
            var property = await _context.Properties
                .Include(p => p.PropertyImages)
                .Include(p => p.PropertyCategoryMappings)
                .Include(p => p.PropertyFeatures)
                .Include(p => p.SeoInfo)
                .FirstOrDefaultAsync(p => p.Id == propertyId);

            if (property == null)
                return NotFound("Không tìm thấy bất động sản.");

            if (property.UserId != userId && userRole != "Admin")
                return StatusCode(403, new { message = "Bạn không có quyền xóa bất động sản này." });

            // Lưu thông tin property trước khi xóa để ghi log
            var propertyToLog = new
            {
                property.Id,
                property.Title,
                property.Description,
                property.Address,
                property.Price,
                property.Area,
                property.Bedrooms,
                property.Bathrooms,
                property.Latitude,
                property.Longitude,
                property.CreatedAt,
                Categories = property.PropertyCategoryMappings.Select(m => m.CategoryId).ToList(),
                Features = property.PropertyFeatures.Select(f => new { f.FeatureName, f.FeatureValue }).ToList(),
                Images = property.PropertyImages.Select(i => new { i.ImageUrl, i.IsPrimary, i.SortOrder }).ToList(),
                SeoInfo = property.SeoInfo != null ? new
                {
                    property.SeoInfo.MetaTitle,
                    property.SeoInfo.MetaDescription,
                    property.SeoInfo.MetaKeywords,
                    property.SeoInfo.CanonicalUrl
                } : null
            };

            // Xóa ảnh liên quan nếu có
            foreach (var image in property.PropertyImages)
            {
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", image.ImageUrl.Replace("/images/", "").TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                    System.IO.File.Delete(imagePath);

                _context.PropertyImages.Remove(image);
            }
            var relatedFavorites = _context.Favorites.Where(f => f.PropertyId == propertyId);
            _context.Favorites.RemoveRange(relatedFavorites);
            // Xóa các thông tin liên quan
            _context.PropertyCategoryMappings.RemoveRange(property.PropertyCategoryMappings);
            _context.PropertyFeatures.RemoveRange(property.PropertyFeatures);

            if (property.SeoInfo != null)
                _context.SeoInfos.Remove(property.SeoInfo);

            // Xóa bất động sản
            _context.Properties.Remove(property);

            // Ghi log trước khi SaveChanges để đảm bảo property còn tồn tại
            await _auditLogService.LogActionAsync(
                "Delete",
                "Properties",
                propertyId,
                JsonSerializer.Serialize(propertyToLog),
                null);

            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa bất động sản thành công." });
        }
        [HttpPost("report")]
        [Authorize]
        public async Task<IActionResult> SubmitReport([FromBody] ReportPostDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.Reason))
                {
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });
                }

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                    return Unauthorized("Không tìm thấy user trong token.");

                int userId = int.Parse(userIdClaim);

                var propertyExists = await _context.Properties.AnyAsync(p => p.Id == dto.PropertyId);
                if (!propertyExists)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy bài đăng." });
                }

                var report = new ReportPost
                {
                    PropertyId = dto.PropertyId,
                    UserId = userId,
                    Reason = dto.Reason,
                    Note = dto.Note,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.ReportPosts.Add(report);
                await _context.SaveChangesAsync();

                // CHỈ GHI LOG KHI TẠO REPORT THÀNH CÔNG
                await _auditLogService.LogActionAsync(
                    "CreateReport",
                    "ReportPosts",
                    report.Id,
                    null,
                    JsonSerializer.Serialize(new
                    {
                        PropertyId = report.PropertyId,
                        UserId = report.UserId,
                        Reason = report.Reason,
                        Status = report.Status,
                        CreatedAt = report.CreatedAt
                    }));

                return Ok(new { success = true, message = "Gửi báo cáo thành công." });
            }
            catch (Exception ex)
            {
                await _auditLogService.LogActionAsync(
            "ReportError",
            "System",
            null,
            null,
            JsonSerializer.Serialize(new
            {
                Error = ex.Message,
                PropertyId = dto?.PropertyId,
                UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            }));

                return StatusCode(500, new { success = false, message = "Lỗi hệ thống.", detail = ex.Message });
            }
        }
    }
}
