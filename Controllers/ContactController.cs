using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using thuctap2025.Data;
using Microsoft.EntityFrameworkCore;
using thuctap2025.Models;

namespace thuctap2025.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ContactController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("contact")]
        [AllowAnonymous] 
        public async Task<IActionResult> GetSettings()
        {
            var setting = await _context.ContactPageSettings.FirstOrDefaultAsync();
            if (setting == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Contact page settings not found."
                });
            }

            return Ok(setting);
        }

        [HttpPost("upsert")]
        public async Task<IActionResult> UpsertSettings([FromBody] ContactPageSettings request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var existing = await _context.ContactPageSettings.FirstOrDefaultAsync();

                if (existing == null)
                {
                    request.CreatedAt = DateTime.UtcNow;
                    _context.ContactPageSettings.Add(request);
                }
                else
                {
                    existing.Address = request.Address;
                    existing.Email = request.Email;
                    existing.Phone = request.Phone;
                    existing.MapEmbedUrl = request.MapEmbedUrl;
                    existing.OpeningHours = request.OpeningHours;
                    existing.Facebook = request.Facebook;
                    existing.Zalo = request.Zalo;
                    existing.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Contact settings saved successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while saving settings",
                    error = ex.Message
                });
            }
        }
    }
}
