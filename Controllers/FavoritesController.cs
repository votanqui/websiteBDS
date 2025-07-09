using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using thuctap2025.Data;
using thuctap2025.DTOs;
using thuctap2025.Models;

namespace thuctap2025.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FavoritesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FavoritesController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpPost("AddFavorite")]
        [Authorize]
        public async Task<ActionResult> CreateFavorite(FavoriteDTO dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || dto.UserId.ToString() != userIdClaim)
                return Forbid();

            var exists = await _context.Favorites.AnyAsync(f =>
                f.PropertyId == dto.PropertyId && f.UserId == dto.UserId);

            if (exists)
                return Conflict(new { message = "Đã có trong danh sách yêu thích." });

            var favorite = new Favorite
            {
                PropertyId = dto.PropertyId,
                UserId = dto.UserId,
                CreatedAt = DateTime.Now
            };

            _context.Favorites.Add(favorite);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã thêm vào yêu thích." });
        }
        [HttpGet("simple/user/{userId}")]
        [Authorize]
        public async Task<IActionResult> GetSimpleUserFavorites(int userId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || userId.ToString() != userIdClaim)
                return Forbid();

            var favorites = await _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Property)
                .Select(f => new
                {
                    PropertyId = f.Property.Id,
                    Title = f.Property.Title
                })
                .ToListAsync();

            return Ok(favorites);
        }


        [HttpGet("user/{userId}/favorites")]
        [Authorize]
        public async Task<IActionResult> GetUserFavorites(int userId, int page = 1, int pageSize = 10)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || userId.ToString() != userIdClaim)
                return StatusCode(403, new { message = "Bạn không có quyền truy cập dữ liệu người dùng khác." });

            var favoritesQuery = _context.Favorites
                .Where(f => f.UserId == userId)
                .Include(f => f.Property)
                    .ThenInclude(p => p.PropertyImages)
                .Include(f => f.Property)
                    .ThenInclude(p => p.PropertyCategoryMappings)
                        .ThenInclude(m => m.Category)
                .OrderByDescending(f => f.CreatedAt);

            var totalFavorites = await favoritesQuery.CountAsync();

            var favorites = await favoritesQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var favoriteResult = favorites.Select(f => new
            {
                PropertyId = f.Property.Id,
                Title = f.Property.Title,
                Price = f.Property.Price,
                Location = f.Property.Address ?? "Chưa rõ",
                Beds = f.Property.Bedrooms,
                Baths = f.Property.Bathrooms,
                Area = f.Property.Area,
                CreatedAt = f.Property.CreatedAt,
                IsVip = f.Property.IsVip,
                Image = f.Property.PropertyImages
                    .Where(img => img.IsPrimary)
                    .OrderBy(img => img.SortOrder)
                    .Select(img => img.ImageUrl)
                    .FirstOrDefault() ?? "default.jpg",
                Categories = f.Property.PropertyCategoryMappings
                    .Select(m => m.Category.Name)
                    .ToList()
            });

            return Ok(new
            {
                currentPage = page,
                pageSize = pageSize,
                totalItems = totalFavorites,
                totalPages = (int)Math.Ceiling((double)totalFavorites / pageSize),
                data = favoriteResult
            });
        }

        [HttpGet("user/{userId}/recently-viewed")]
        [Authorize]
        public async Task<IActionResult> GetUserRecentlyViewed(int userId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || userId.ToString() != userIdClaim)
                return StatusCode(403, new { message = "Bạn không có quyền truy cập dữ liệu người dùng khác." });

            var oneWeekAgo = DateTime.Now.AddDays(-7);

            var recentViewsRaw = await _context.PropertyViews
                .Where(v => v.UserId == userId && v.ViewedAt >= oneWeekAgo)
                .OrderByDescending(v => v.ViewedAt)
                .Include(v => v.Property)
                    .ThenInclude(p => p.PropertyImages)
                .Include(v => v.Property)
                    .ThenInclude(p => p.PropertyCategoryMappings)
                        .ThenInclude(m => m.Category)
                .Take(1000)
                .ToListAsync();

            var viewedResult = recentViewsRaw
                .GroupBy(v => v.Property.Id)
                .Select(g => g.First())
                .Take(10)
                .Select(v => new
                {
                    PropertyId = v.Property?.Id ?? 0,
                    Title = v.Property?.Title ?? "Không rõ",
                    Price = v.Property?.Price ?? 0,
                    Location = v.Property?.Address ?? "Chưa rõ",
                    Beds = v.Property?.Bedrooms ?? 0,
                    Baths = v.Property?.Bathrooms ?? 0,
                    Area = v.Property?.Area ?? 0,
                    CreatedAt = v.Property?.CreatedAt,
                    IsVip = v.Property.IsVip,
                    Image = v.Property?.PropertyImages?
                        .Where(img => img.IsPrimary)
                        .OrderBy(img => img.SortOrder)
                        .Select(img => img.ImageUrl)
                        .FirstOrDefault() ?? "default.jpg",
                    Categories = v.Property?.PropertyCategoryMappings?
                        .Select(m => m.Category?.Name)
                        .Where(name => !string.IsNullOrEmpty(name))
                        .ToList() ?? new List<string>()
                })
                .ToList();

            return Ok(viewedResult);
        }

        [HttpDelete("DeleteFavorite/{propertyId}")]
        [Authorize]
        public async Task<IActionResult> DeleteFavorite(int propertyId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var favorite = await _context.Favorites
                .FirstOrDefaultAsync(f => f.PropertyId == propertyId && f.UserId == userId);

            if (favorite == null)
                return NotFound(new { message = "Không tìm thấy mục yêu thích để xóa." });

            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa khỏi danh sách yêu thích." });
        }

    }
}
