using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using thuctap2025.Data;
using thuctap2025.DTOs;
using thuctap2025.Models;

namespace thuctap2025.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CategoriesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PropertyCategoryDTO>>> GetCategories()
        {
            var categories = await _context.PropertyCategories
                .Select(c => new PropertyCategoryDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return categories;
        }

        // GET: api/Categories/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PropertyCategoryDTO>> GetCategory(int id)
        {
            var category = await _context.PropertyCategories.FindAsync(id);
            if (category == null) return NotFound();

            return new PropertyCategoryDTO
            {
                Id = category.Id,
                Name = category.Name,
                Slug = category.Slug,
                CreatedAt = category.CreatedAt
            };
        }

        // POST: api/Categories
        [HttpPost]
        public async Task<ActionResult<PropertyCategoryDTO>> CreateCategory(PropertyCategoryDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Slug))
                return BadRequest(new { message = "Name and Slug are required." });

            var category = new PropertyCategory
            {
                Name = dto.Name,
                Slug = dto.Slug,
                CreatedAt = DateTime.Now
            };

            _context.PropertyCategories.Add(category);
            await _context.SaveChangesAsync();

            dto.Id = category.Id;
            dto.CreatedAt = category.CreatedAt;

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, dto);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, PropertyCategoryDTO dto)
        {
            if (id != dto.Id)
                return BadRequest(new { message = "Id mismatch." });

            var category = await _context.PropertyCategories.FindAsync(id);
            if (category == null)
                return NotFound(new { message = "Category not found." });

            category.Name = dto.Name;
            category.Slug = dto.Slug;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.PropertyCategories.FindAsync(id);
            if (category == null)
                return NotFound(new { message = "Category not found." });

            bool isInUse = await _context.PropertyCategoryMappings
                .AnyAsync(m => m.CategoryId == id);
            if (isInUse)
                return Conflict(new { message = "Không thể xóa vì Category đang được sử dụng." });

            _context.PropertyCategories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
