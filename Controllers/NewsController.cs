using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using thuctap2025.Data;
using thuctap2025.Models;
using System.Text.Json;
using thuctap2025.DTOs;

namespace thuctap2025.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;
        public NewsController(ApplicationDbContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }
        [HttpPut("publish/{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult UpdatePublishStatus(int id, [FromBody] bool isPublished)
        {
            var newsItem = _context.News.FirstOrDefault(n => n.Id == id);
            if (newsItem == null)
            {
                return NotFound(new { message = "Không tìm thấy bài viết." });
            }

            newsItem.IsPublished = isPublished;
            newsItem.PublishedAt = isPublished ? DateTime.Now : null;

            _context.SaveChanges();

            return Ok(new
            {
                message = "Cập nhật trạng thái bài viết thành công.",
                newsItem.Id,
                newsItem.IsPublished,
                newsItem.PublishedAt
            });
        }

        [HttpGet("allnews")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetAllNewsForAdmin(
      [FromQuery] int page = 1,
      [FromQuery] int pageSize = 10,
      [FromQuery] string? searchTerm = null,
      [FromQuery] string? status = null)
        {
            var query = _context.News
                .Include(n => n.NewsImages)
                .Include(n => n.NewsTagMappings).ThenInclude(m => m.Tag)
                .Include(n => n.Category)
                .Include(n => n.Author)
                .AsQueryable();

            // Lọc theo status nếu có
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status.ToLower() == "published")
                    query = query.Where(n => n.IsPublished == true);
                else if (status.ToLower() == "unpublished")
                    query = query.Where(n => n.IsPublished == false);
            }

            // Lọc theo searchTerm nếu có
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowerSearch = searchTerm.ToLower();
                query = query.Where(n =>
                    (!string.IsNullOrEmpty(n.Title) && n.Title.ToLower().Contains(lowerSearch)) ||
                    (n.Author != null && n.Author.FullName.ToLower().Contains(lowerSearch)) ||
                    (n.Category != null && n.Category.Name.ToLower().Contains(lowerSearch)));
            }

            query = query.OrderByDescending(n => n.CreatedAt);

            var totalItems = query.Count();

            var newsList = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = newsList.Select(n => new
            {
                Id = n.Id,
                Title = n.Title,
                Slug = n.Slug,
                ShortDescription = n.ShortDescription,
                ViewCount = n.ViewCount,
                IsPublished = n.IsPublished,
                PublishedAt = n.PublishedAt,
                CreatedAt = n.CreatedAt,
                UpdatedAt = n.UpdatedAt,
                Author = n.Author.FullName,
                Category = n.Category != null ? n.Category.Name : "Không có danh mục",
                FeaturedImage = n.NewsImages
                    .Where(img => img.IsFeatured)
                    .OrderBy(img => img.SortOrder)
                    .Select(img => img.ImageUrl)
                    .FirstOrDefault() ?? "default-news.jpg",
                Tags = n.NewsTagMappings.Select(m => m.Tag.Name).ToList()
            });

            return Ok(new
            {
                data = result,
                totalItems,
                currentPage = page,
                pageSize
            });
        }


        [HttpGet("tags")]
        public async Task<IActionResult> GetTags()
        {
            var tags = await _context.NewsTags
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Slug,
                    t.CreatedAt
                })
                .ToListAsync();

            return Ok(tags);
        }

        // GET: api/news/categories
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.NewsCategories
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Slug,
                    c.Description,
                    c.CreatedAt,
                    c.UpdatedAt
                })
                .ToListAsync();

            return Ok(categories);
        }
        [HttpPost("UploadImagesNews")]
        [Authorize]
        public async Task<IActionResult> UploadImagesNews([FromForm] List<IFormFile> files)

        {
            if (files == null || files.Count == 0)
                return BadRequest("No files uploaded");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images-news");
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

                uploadedUrls.Add($"/images-news/{uniqueFileName}");
            }

            return Ok(uploadedUrls);
        }
        [Authorize]
        [HttpDelete("RemoveImageNews")]
        public async Task<IActionResult> RemoveImageNews([FromQuery] string imageName)
        {
            if (string.IsNullOrEmpty(imageName))
                return BadRequest("Tên ảnh không được để trống.");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            int userId = int.Parse(userIdClaim);

            // So sánh đúng ImageUrl trong DB
            var image = await _context.NewsImages
                .Include(pi => pi.News)
                .FirstOrDefaultAsync(pi => pi.ImageUrl == imageName);

            if (image == null)
                return NotFound("Ảnh không tồn tại.");

            if (userRole != "Admin")
                return StatusCode(403, new { message = "Bạn không có quyền xóa ảnh này." });
            // Cắt /images/ để xóa file
            var fileNameOnly = imageName.Replace("/images-news/", "").TrimStart('/');
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images-news", fileNameOnly);
            if (System.IO.File.Exists(imagePath))
                System.IO.File.Delete(imagePath);

            _context.NewsImages.Remove(image);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa ảnh thành công." });
        }
        [HttpPost("CreateNews")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateNews([FromBody] CreateNewsRequest request)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            var userIdClaim = identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "Không thể xác thực người dùng." });
            }

            int authorId = int.Parse(userIdClaim);

            var news = new News
            {
                Title = request.Title,
                Slug = request.Slug,
                ShortDescription = request.ShortDescription,
                Content = request.Content,
                AuthorId = authorId,
                CategoryId = request.CategoryId,
                IsPublished = request.IsPublished,
                PublishedAt = request.PublishedAt,
                MetaTitle = request.MetaTitle,
                MetaDescription = request.MetaDescription,
                MetaKeywords = request.MetaKeywords,
                CreatedAt = DateTime.Now
            };

            _context.News.Add(news);
            await _context.SaveChangesAsync();

            // Thêm hình ảnh
            int sortOrder = 0;
            bool featuredSet = false;

            foreach (var imageUrl in request.ImageUrls)
            {
                bool isFeatured = false;

                if (!featuredSet && imageUrl.Trim() == request.FeaturedImage?.Trim())
                {
                    isFeatured = true;
                    featuredSet = true;
                }

                _context.NewsImages.Add(new NewsImage
                {
                    NewsId = news.Id,
                    ImageUrl = imageUrl,
                    IsFeatured = isFeatured,
                    SortOrder = sortOrder++,
                    CreatedAt = DateTime.Now
                });
            }

            // Gán tag
            foreach (var tagId in request.TagIds.Distinct())
            {
                _context.NewsTagMappings.Add(new NewsTagMapping
                {
                    NewsId = news.Id,
                    TagId = tagId
                });
            }

            await _context.SaveChangesAsync();

            // Ghi log nếu cần
            await _auditLogService.LogActionAsync(
                "Create",
                "News",
                news.Id,
                null,
                JsonSerializer.Serialize(new
                {
                    news.Id,
                    news.Title,
                    news.Slug,
                    news.ShortDescription,
                    news.Content,
                    news.AuthorId,
                    news.CategoryId,
                    news.PublishedAt,
                    request.ImageUrls,
                    request.FeaturedImage,
                    request.TagIds,
                    request.MetaTitle,
                    request.MetaDescription,
                    request.MetaKeywords,                
                })
            );

            return Ok(new
            {
                status = "success",
                data = new { id = news.Id },
                message = "News created successfully"
            });

        }
        [HttpPut("EditNews/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditNews(int id, [FromBody] CreateNewsRequest request)
        {
            var identity = HttpContext.User.Identity as ClaimsIdentity;
            var userIdClaim = identity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "Không thể xác thực người dùng." });
            }

            var news = await _context.News
                .Include(n => n.NewsImages)
                .Include(n => n.NewsTagMappings)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (news == null)
            {
                return NotFound(new { message = "Không tìm thấy bài viết." });
            }

            var oldValues = new
            {
                news.Id,
                news.Title,
                news.Slug,
                news.ShortDescription,
                news.Content,
                news.AuthorId,
                news.CategoryId,
                news.PublishedAt,
                ImageUrls = news.NewsImages.Select(i => i.ImageUrl).ToList(),
                FeaturedImage = news.NewsImages.FirstOrDefault(i => i.IsFeatured)?.ImageUrl,
                TagIds = news.NewsTagMappings.Select(t => t.TagId).ToList(),
                news.MetaTitle,
                news.MetaDescription,
                news.MetaKeywords
            };

            // Cập nhật thông tin chính
            news.Title = request.Title;
            news.Slug = request.Slug;
            news.ShortDescription = request.ShortDescription;
            news.Content = request.Content;
            news.CategoryId = request.CategoryId;
            news.IsPublished = request.IsPublished;
            news.PublishedAt = request.PublishedAt;
            news.MetaTitle = request.MetaTitle;
            news.MetaDescription = request.MetaDescription;
            news.MetaKeywords = request.MetaKeywords;
            news.UpdatedAt = DateTime.Now;

            // Xoá toàn bộ ảnh cũ
            _context.NewsImages.RemoveRange(news.NewsImages);

            // Thêm ảnh mới
            int sortOrder = 0;
            bool featuredSet = false;
            foreach (var imageUrl in request.ImageUrls)
            {
                bool isFeatured = false;

                if (!featuredSet && imageUrl.Trim() == request.FeaturedImage?.Trim())
                {
                    isFeatured = true;
                    featuredSet = true;
                }

                _context.NewsImages.Add(new NewsImage
                {
                    NewsId = news.Id,
                    ImageUrl = imageUrl,
                    IsFeatured = isFeatured,
                    SortOrder = sortOrder++,
                    CreatedAt = DateTime.Now
                });
            }

            // Xoá tag cũ và gán tag mới
            _context.NewsTagMappings.RemoveRange(news.NewsTagMappings);
            foreach (var tagId in request.TagIds.Distinct())
            {
                _context.NewsTagMappings.Add(new NewsTagMapping
                {
                    NewsId = news.Id,
                    TagId = tagId
                });
            }

            await _context.SaveChangesAsync();

            var newValues = new
            {
                news.Id,
                news.Title,
                news.Slug,
                news.ShortDescription,
                news.Content,
                news.AuthorId,
                news.CategoryId,
                news.PublishedAt,
                request.ImageUrls,
                request.FeaturedImage,
                request.TagIds,
                news.MetaTitle,
                news.MetaDescription,
                news.MetaKeywords
            };

            await _auditLogService.LogActionAsync(
                "Update",
                "News",
                news.Id,
                JsonSerializer.Serialize(oldValues),
                JsonSerializer.Serialize(newValues)
            );

            return Ok(new
            {
                status = "success",
                message = "Cập nhật bài viết thành công",
                data = new { id = news.Id }
            });
        }
        [HttpGet("GetNews/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetNews(int id)
        {
            var news = await _context.News
                .Include(n => n.NewsImages)
                .Include(n => n.NewsTagMappings)
                    .ThenInclude(nt => nt.Tag) // nếu có bảng Tags
                .Include(n => n.Category)
                .Include(n => n.Author)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (news == null)
            {
                return NotFound(new { message = "Không tìm thấy bài viết." });
            }

            var result = new
            {
                news.Id,
                news.Title,
                news.Slug,
                news.ShortDescription,
                news.Content,
                news.CategoryId,
                CategoryName = news.Category?.Name,
                news.IsPublished,
                news.PublishedAt,
                news.CreatedAt,
                news.UpdatedAt,
                AuthorId = news.AuthorId,
                AuthorName = news.Author?.FullName ?? "Không rõ",

                Meta = new
                {
                    news.MetaTitle,
                    news.MetaDescription,
                    news.MetaKeywords
                },

                Images = news.NewsImages
                    .OrderBy(i => i.SortOrder)
                    .Select(i => new
                    {
                        i.Id,
                        i.ImageUrl,
                        i.IsFeatured,
                        i.SortOrder,
                        i.CreatedAt
                    }).ToList(),

                FeaturedImage = news.NewsImages.FirstOrDefault(i => i.IsFeatured)?.ImageUrl,

                Tags = news.NewsTagMappings
                    .Select(t => new
                    {
                        t.TagId,
                        TagName = t.Tag?.Name ?? "Không rõ"
                    }).ToList()
            };

            return Ok(new
            {
                status = "success",
                data = result
            });
        }
        [Authorize(Roles = "Admin")]
        [HttpDelete("DeleteNews/{id}")]
        public async Task<IActionResult> DeleteNews(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized("Không xác định được người dùng.");

            
            var news = await _context.News
                .Include(n => n.NewsImages) 
                .Include(n => n.NewsTagMappings) 
                .FirstOrDefaultAsync(n => n.Id == id);

            if (news == null)
                return NotFound("Tin tức không tồn tại.");

           
            if (news.AuthorId != userId && userRole != "Admin")
                return StatusCode(403, new { message = "Bạn không có quyền xóa tin tức này." });

            foreach (var image in news.NewsImages)
            {
                var fileNameOnly = image.ImageUrl.Replace("/images-news/", "").TrimStart('/');
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images-news", fileNameOnly);
                if (System.IO.File.Exists(imagePath))
                    System.IO.File.Delete(imagePath);
            }

            
            _context.News.Remove(news);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa tin tức và dữ liệu liên quan thành công." });
        }
        [AllowAnonymous]
        [HttpGet("GetNewsSummary")]
        public async Task<IActionResult> GetNewsSummary()
        {
            var newsList = await _context.News
                .Where(n => n.IsPublished)
                .Select(n => new
                {
                    Id = n.Id,
                    Title = n.Title,
                    ShortDescription = n.ShortDescription,
                    PublishedAt = n.PublishedAt,
                    FeaturedImage = n.NewsImages
                        .Where(img => img.IsFeatured)
                        .Select(img => img.ImageUrl)
                        .FirstOrDefault()
                })
                .OrderByDescending(n => n.PublishedAt)
                .ToListAsync();

            return Ok(newsList);
        }
        [AllowAnonymous]
        [HttpGet("GetNewsDetail/{id}")]
        public async Task<IActionResult> GetNewsDetail(int id)
        {
            var news = await _context.News
                .Include(n => n.NewsImages)
                .Include(n => n.Category)
                .Include(n => n.Author) // giả định có navigation đến Author
                .Include(n => n.NewsTagMappings)
                    .ThenInclude(tm => tm.Tag)
                .Where(n => n.IsPublished && n.Id == id)
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Slug,
                    n.ShortDescription,
                    n.Content,
                    n.PublishedAt,
                    n.MetaTitle,
                    n.MetaDescription,
                    n.MetaKeywords,
                    Category = n.Category != null ? n.Category.Name : null,
                    Author = n.Author != null ? n.Author.FullName : null,
                    Images = n.NewsImages.Select(img => new
                    {
                        img.ImageUrl,
                        img.IsFeatured,
                        img.SortOrder
                    }).OrderBy(img => img.SortOrder).ToList(),
                    Tags = n.NewsTagMappings.Select(tm => tm.Tag.Name).ToList()
                })
                .FirstOrDefaultAsync();

            if (news == null)
                return NotFound(new { message = "Không tìm thấy tin tức." });

            return Ok(news);
        }

        [AllowAnonymous]
        [HttpGet("GetFilteredNews")]
        public async Task<IActionResult> GetFilteredNews(
      [FromQuery] int page = 1,
      [FromQuery] int pageSize = 10,
      [FromQuery] int? categoryId = null,
      [FromQuery] int? tagId = null)
        {
            var query = _context.News
                .Where(n => n.IsPublished)
                .AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(n => n.CategoryId == categoryId.Value);
            }

            if (tagId.HasValue)
            {
                // Lọc bài viết có chứa tag cụ thể
                query = query.Where(n =>
                    _context.NewsTagMappings.Any(m => m.NewsId == n.Id && m.TagId == tagId.Value)
                );
            }

            var totalItems = await query.CountAsync();

            var items = await query
                .OrderByDescending(n => n.PublishedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new
                {
                    Id = n.Id,
                    Title = n.Title,
                    ShortDescription = n.ShortDescription,
                    PublishedAt = n.PublishedAt,
                    CategoryId = n.CategoryId,
                    CategoryName = n.Category.Name, // Lấy tên danh mục
                    FeaturedImage = n.NewsImages
                        .Where(img => img.IsFeatured)
                        .Select(img => img.ImageUrl)
                        .FirstOrDefault(),
                    Tags = _context.NewsTagMappings
                        .Where(m => m.NewsId == n.Id)
                        .Select(m => m.Tag.Name)
                        .ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize,
                Items = items
            });
        }

    }
}
