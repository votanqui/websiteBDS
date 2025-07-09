using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using thuctap2025.Data;
using thuctap2025.Models;
using thuctap2025.DTOs;

namespace thuctap2025.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
    

        public HomeController(ApplicationDbContext context, AuthService authService)
        {
            _context = context;
          
        }
        [HttpGet("compare")]
        public async Task<IActionResult> CompareProperties([FromQuery] int id1, [FromQuery] int id2)
        {
            var properties = await _context.Properties
                .Where(p => p.Id == id1 || p.Id == id2)
                .Include(p => p.PropertyImages)
                .Include(p => p.PropertyCategoryMappings).ThenInclude(m => m.Category)
                .Include(p => p.PropertyFeatures)
                .ToListAsync();

            if (properties.Count != 2)
                return NotFound(new { message = "Không tìm thấy đủ 2 bất động sản để so sánh." });

            var result = properties.Select(p => new
            {
                id = p.Id,
                title = p.Title,
                address = p.Address,
                price = p.Price,
                area = p.Area,
                bedrooms = p.Bedrooms,
                bathrooms = p.Bathrooms,
                isVip = p.IsVip,
                image = p.PropertyImages
                    .FirstOrDefault(img => img.IsPrimary)?.ImageUrl
                    ?? p.PropertyImages.FirstOrDefault()?.ImageUrl,
                categories = p.PropertyCategoryMappings
                    .Select(c => c.Category.Name).ToList(),
                features = p.PropertyFeatures
                    .Select(f => $"{f.FeatureName}: {f.FeatureValue}").ToList()
            }).ToList();

            return Ok(result);
        }

        [HttpGet("featured")]
        public IActionResult GetFeaturedProperties()
        {
            // Step 1: Load từ DB (chỉ lấy cần thiết để tránh nặng bộ nhớ)
            var allViews = _context.PropertyViews
                .Include(pv => pv.Property).ThenInclude(p => p.PropertyImages)
                .Where(pv => pv.Property.Status == "Approved")
                .ToList(); // ép về memory

            // Step 2: Group và xử lý trong memory
            var mostViewed = allViews
                .GroupBy(pv => pv.PropertyId)
                .Select(g => new
                {
                    Count = g.Count(),
                    Property = g.First().Property
                })
                .OrderByDescending(x => x.Property.IsVip)
                .ThenByDescending(x => x.Count)
                .Take(10)
                .Select(x => new FeaturedPropertyDto
                {
                    Id = x.Property.Id,
                    Title = x.Property.Title,
                    Price = x.Property.Price,
                    Location = x.Property.Address ?? "Chưa rõ",
                    Beds = x.Property.Bedrooms,
                    Baths = x.Property.Bathrooms,
                    Area = x.Property.Area,
                    IsVip = x.Property.IsVip,
                    Image = x.Property.PropertyImages
                                .Where(img => img.IsPrimary)
                                .OrderBy(img => img.SortOrder)
                                .Select(img => img.ImageUrl)
                                .FirstOrDefault() ?? "default.jpg"
                })
                .ToList();

            // Pinned - không lỗi vì không dùng GroupBy
            var pinned = _context.Properties
                .Include(p => p.PropertyImages)
                .Where(p => p.Status == "Pinned")
                .OrderByDescending(p => p.IsVip)
                .ThenByDescending(p => p.CreatedAt)
                .Take(10)
                .Select(p => new FeaturedPropertyDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    Price = p.Price,
                    Location = p.Address ?? "Chưa rõ",
                    Beds = p.Bedrooms,
                    Baths = p.Bathrooms,
                    Area = p.Area,
                    IsVip = p.IsVip,
                    Image = p.PropertyImages
                                .Where(img => img.IsPrimary)
                                .OrderBy(img => img.SortOrder)
                                .Select(img => img.ImageUrl)
                                .FirstOrDefault() ?? "default.jpg"
                })
                .ToList();

            return Ok(new
            {
                MostViewed = mostViewed,
                Pinned = pinned
            });
        }


        [HttpGet("detail/{id}")]
        public async Task<IActionResult> GetPropertyDetail(int id)
        {
            var property = await _context.Properties
                .Include(p => p.User)
                .Include(p => p.PropertyCategoryMappings)
                    .ThenInclude(m => m.Category)
                .Include(p => p.PropertyFeatures)
                .Include(p => p.PropertyImages)
                .Include(p => p.SeoInfo) // ✅ Thêm dòng này
                .FirstOrDefaultAsync(p => p.Id == id);

            if (property == null)
                return NotFound();

        var dto = new PropertyDetailDto
            {
                Id = property.Id,
                Title = property.Title,
                Description = property.Description,
                Address = property.Address,
                Price = property.Price,
                Area = property.Area,
                Bedrooms = property.Bedrooms,
                Bathrooms = property.Bathrooms,
                 Latitude = property.Latitude,
                Longitude = property.Longitude,
                 Status = property.Status,
                CreatedAt = property.CreatedAt,
                UserName = property.User?.FullName,
                 AvatarUrl = property.User?.AvatarUrl,
                  phone = property.User?.PhoneNumber,
                UserId= property.UserId,
                IsVip = property.IsVip,
            Categories = property.PropertyCategoryMappings
                    .Select(c => c.Category.Name).ToList(),
                Features = property.PropertyFeatures
                    .Select(f => $"{f.FeatureName}: {f.FeatureValue}").ToList(),
                Images = property.PropertyImages
                    .Select(i => i.ImageUrl).ToList(),
                MainImage = property.PropertyImages
                    .FirstOrDefault(i => i.IsPrimary)?.ImageUrl
                    ?? property.PropertyImages.FirstOrDefault()?.ImageUrl,

                // ✅ Thêm các trường SEO
                MetaTitle = property.SeoInfo?.MetaTitle,
                MetaDescription = property.SeoInfo?.MetaDescription,
                MetaKeywords = property.SeoInfo?.MetaKeywords,
                CanonicalUrl = property.SeoInfo?.CanonicalUrl
            };

            return Ok(dto);
        }
        [HttpGet("Properties")]
        public IActionResult GetProperties(
            string? location,
            decimal? minPrice,
            decimal? maxPrice,
            double? minArea,
            double? maxArea,
            int? minBeds,
            int? maxBeds,
            string? status,
            string? category,
            int page = 1,
            int pageSize = 10)
        {
            var query = _context.Properties
                .Include(p => p.PropertyImages)
                .Include(p => p.PropertyCategoryMappings).ThenInclude(m => m.Category)
                .Where(p => p.Status == "Approved")
                .AsQueryable();

            // 🔍 Lọc theo các điều kiện
            if (!string.IsNullOrWhiteSpace(location))
            {
                var trimmedLocation = location.Trim();
                query = query.Where(p => p.Address.Trim().Contains(trimmedLocation));
            }

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            if (minArea.HasValue)
                query = query.Where(p => p.Area >= minArea.Value);

            if (maxArea.HasValue)
                query = query.Where(p => p.Area <= maxArea.Value);

            if (minBeds.HasValue)
                query = query.Where(p => p.Bedrooms >= minBeds.Value);

            if (maxBeds.HasValue)
                query = query.Where(p => p.Bedrooms <= maxBeds.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.PropertyCategoryMappings
                    .Any(m => m.Category.Slug == category));

            // 🔢 Tổng số kết quả
            var totalItems = query.Count();

            // 📄 Lấy theo trang
            var propertyList = query
                .OrderByDescending(p => p.IsVip)
                .ThenByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var properties = propertyList
                .Select(p => new
                {
                    Id = p.Id,
                    Title = p.Title,
                    Price = p.Price,
                    Location = p.Address ?? "Chưa rõ",
                    Beds = p.Bedrooms,
                    Baths = p.Bathrooms,
                    IsVip = p.IsVip,
                    Area = p.Area,
                    Image = p.PropertyImages
                                .Where(img => img.IsPrimary)
                                .OrderBy(img => img.SortOrder)
                                .Select(img => img.ImageUrl)
                                .FirstOrDefault() ?? "default.jpg",
                    Categories = p.PropertyCategoryMappings.Select(m => m.Category.Name).ToList(),
                    Date = p.CreatedAt.ToString("dd/MM/yyyy")
                })
                .ToList();

            return Ok(new
            {
                data = properties,
                totalItems,
                currentPage = page,
                pageSize
            });
        }


        [HttpPost("track")]
        public async Task<IActionResult> TrackView([FromBody] PropertyViewRequest request)
        {
            var ip = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                     ?? HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            int? userId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int parsedUserId))
            {
                userId = parsedUserId;
            }

            var property = await _context.Properties
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.PropertyId);

            if (property == null)
                return NotFound("Property not found.");

            var cutoffTime = DateTime.Now.AddMinutes(-60);
            var isSpam = await _context.PropertyViews.AnyAsync(v =>
                v.PropertyId == request.PropertyId &&
                (
                    (userId != null && v.UserId == userId && v.ViewedAt > cutoffTime) ||
                    (v.IPAddress == ip && v.ViewedAt > cutoffTime)
                )
            );

            if (isSpam)
            {
                return Ok(new { message = "Already tracked recently." });
            }

            // Tạo danh sách lượt xem
            var views = new List<PropertyView>();

            // 1 lượt luôn có
            views.Add(new PropertyView
            {
                PropertyId = request.PropertyId,
                UserId = userId,
                IPAddress = ip,
                UserAgent = userAgent
            });

            // Nếu VIP thì thêm lượt nữa
            if (property.IsVip)
            {
                views.Add(new PropertyView
                {
                    PropertyId = request.PropertyId,
                    UserId = userId,
                    IPAddress = ip,
                    UserAgent = userAgent
                });
            }

            // Lưu tất cả
            _context.PropertyViews.AddRange(views);
            await _context.SaveChangesAsync();

            return Ok(new { message = "View tracked successfully." });
        }
        [HttpPost("track-news")]
        public async Task<IActionResult> TrackNewsView([FromBody] NewsViewRequest request)
        {
            var ip = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                     ?? HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            int? userId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int parsedUserId))
            {
                userId = parsedUserId;
            }

            var news = await _context.News
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == request.NewsId);

            if (news == null)
                return NotFound("News not found.");

            var cutoffTime = DateTime.Now.AddMinutes(-60);

            var isSpam = await _context.NewsViews.AnyAsync(v =>
                v.NewsId == request.NewsId &&
                (
                    (userId != null && v.UserId == userId && v.ViewedAt > cutoffTime) ||
                    (v.IPAddress == ip && v.ViewedAt > cutoffTime)
                )
            );

            if (isSpam)
            {
                return Ok(new { message = "Already tracked recently." });
            }

            // Lưu view
            var view = new NewsView
            {
                NewsId = request.NewsId,
                UserId = userId,
                IPAddress = ip,
                UserAgent = userAgent
            };

            _context.NewsViews.Add(view);
            await _context.SaveChangesAsync();

            return Ok(new { message = "News view tracked successfully." });
        }
        [HttpPost("track-profile")]
        public async Task<IActionResult> TrackUserProfileView([FromBody] UserProfileViewRequest request)
        {
            var ip = HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                     ?? HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            int? viewerUserId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int parsedUserId))
            {
                viewerUserId = parsedUserId;
            }

            var targetUser = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == request.ViewedUserId);

            if (targetUser == null)
                return NotFound("Viewed user not found.");

            var cutoffTime = DateTime.Now.AddMinutes(-60);

            var isSpam = await _context.UserProfileViews.AnyAsync(v =>
                v.ViewedUserId == request.ViewedUserId &&
                (
                    (viewerUserId != null && v.ViewerUserId == viewerUserId && v.ViewedAt > cutoffTime) ||
                    (v.IPAddress == ip && v.ViewedAt > cutoffTime)
                )
            );

            if (isSpam)
            {
                return Ok(new { message = "Already tracked recently." });
            }

            // Lưu view
            var view = new UserProfileView
            {
                ViewedUserId = request.ViewedUserId,
                ViewerUserId = viewerUserId,
                IPAddress = ip,
                UserAgent = userAgent
            };

            _context.UserProfileViews.Add(view);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User profile view tracked successfully." });
        }




        [HttpGet("properties/similar/{id}")]
        public IActionResult GetSimilarProperties(int id)
        {
            var current = _context.Properties
                .Include(p => p.PropertyImages)
                .Include(p => p.PropertyCategoryMappings).ThenInclude(m => m.Category)
                .FirstOrDefault(p => p.Id == id && p.Status == "Approved");

            if (current == null || current.Latitude == null || current.Longitude == null)
                return NotFound("Không tìm thấy bất động sản hoặc thiếu tọa độ.");

            double currentLat = current.Latitude.Value;
            double currentLon = current.Longitude.Value;

            var similar = _context.Properties
                .Include(p => p.PropertyImages)
                .Include(p => p.PropertyCategoryMappings).ThenInclude(m => m.Category)
                .Where(p =>
                    p.Status == "Approved" &&
                    p.Id != id &&
                    p.Latitude != null &&
                    p.Longitude != null)
                .ToList()
                .Where(p => CalculateDistance(currentLat, currentLon, p.Latitude.Value, p.Longitude.Value) <= 10) // trong bán kính 10km
                .Select(p => new
                {
                    Id = p.Id,
                    Title = p.Title,
                    Price = p.Price,
                    Location = p.Address ?? "Chưa rõ",
                    Beds = p.Bedrooms,
                    Baths = p.Bathrooms,
                    Area = p.Area,
                    Image = p.PropertyImages
                        .Where(img => img.IsPrimary)
                        .OrderBy(img => img.SortOrder)
                        .Select(img => img.ImageUrl)
                        .FirstOrDefault() ?? "default.jpg",
                    Categories = p.PropertyCategoryMappings.Select(m => m.Category.Name).ToList(),
                    Distance = Math.Round(CalculateDistance(currentLat, currentLon, p.Latitude.Value, p.Longitude.Value), 2),
                    Date = p.CreatedAt.ToString("dd/MM/yyyy")
                })
                .OrderBy(p => p.Distance) // gần nhất lên đầu
                .Take(10) // giới hạn kết quả
                .ToList();

            return Ok(similar);
        }


        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            double R = 6371; // bán kính trái đất (km)
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }


        private double ToRadians(double angle) => angle * Math.PI / 180;

    }
}
