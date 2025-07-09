using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using thuctap2025.Data;
using thuctap2025.Models;

namespace thuctap2025.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class NewsCategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NewsCategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NewsCategory>>> GetAll()
        {
            return await _context.NewsCategories.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NewsCategory>> GetById(int id)
        {
            var category = await _context.NewsCategories.FindAsync(id);
            if (category == null)
                return NotFound();

            return category;
        }

        [HttpPost]
        public async Task<ActionResult<NewsCategory>> Create(NewsCategory category)
        {
            if (string.IsNullOrWhiteSpace(category.Name) || string.IsNullOrWhiteSpace(category.Slug))
                return BadRequest(new { message = "Name and Slug are required." });

            category.CreatedAt = DateTime.Now;

            _context.NewsCategories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, NewsCategory category)
        {
            if (id != category.Id)
                return BadRequest(new { message = "ID không khớp." });

            var existing = await _context.NewsCategories.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = "Danh mục không tồn tại." });

            existing.Name = category.Name;
            existing.Slug = category.Slug;
            existing.Description = category.Description;
            existing.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.NewsCategories.FindAsync(id);
            if (category == null)
                return NotFound(new { message = "Danh mục không tồn tại." });

            bool hasRelatedNews = await _context.News
                .AnyAsync(n => n.CategoryId == id);

            if (hasRelatedNews)
            {
                bool hasMappings = await _context.NewsTagMappings
                    .AnyAsync(m => _context.News.Any(n => n.Id == m.NewsId && n.CategoryId == id));

                if (hasMappings)
                {
                    return BadRequest(new
                    {
                        message = "Không thể xóa vì có bài viết thuộc danh mục này được liên kết với thẻ."
                    });
                }
            }

            _context.NewsCategories.Remove(category);
            await _context.SaveChangesAsync();
            return NoContent();
        }

    }
}
