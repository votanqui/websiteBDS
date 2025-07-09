using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.Security.Claims;
using thuctap2025.Data;
using thuctap2025.Models;

namespace thuctap2025.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class SettingHomesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public SettingHomesController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: api/SettingHomes/default
        [HttpGet("default")]
        [AllowAnonymous]
        public async Task<ActionResult<SettingHome>> GetDefaultSettingHome()
        {
            var setting = await _context.SettingHomes.FirstOrDefaultAsync();
            return setting ?? new SettingHome();
        }

        [HttpPost("UploadImages")]
        [Authorize]
        public async Task<IActionResult> UploadImages([FromForm] List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                return BadRequest(new { success = false, message = "No files uploaded" });

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images-home");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uploadedUrls = new List<string>();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
                    continue;

                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }
                    uploadedUrls.Add($"/images-home/{uniqueFileName}");
                }
                catch
                {
                    continue;
                }
            }

            return Ok(new { success = true, urls = uploadedUrls });
        }


        [HttpDelete("RemoveImage")]
        public async Task<IActionResult> RemoveImage([FromQuery] string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return BadRequest(new { success = false, message = "Image URL is required" });

            try
            {
                var fileName = Path.GetFileName(imageUrl);
                if (string.IsNullOrEmpty(fileName))
                    return BadRequest(new { success = false, message = "Invalid image URL" });

                var imagePath = Path.Combine(_environment.WebRootPath, "images-home", fileName);

                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }

                return Ok(new { success = true, message = "Image removed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error removing image: {ex.Message}" });
            }
        }

        [HttpPost("default")]
        public async Task<ActionResult> SaveDefaultSettingHome(SettingHome settingHome)
        {
            try
            {
                var existingSetting = await _context.SettingHomes.FirstOrDefaultAsync();
                string oldImageUrl = null;
                string oldOgImageUrl = null;

                if (existingSetting == null)
                {
                    settingHome.CreatedAt = DateTime.Now;
                    _context.SettingHomes.Add(settingHome);
                }
                else
                {
                    oldImageUrl = existingSetting.ImageUrl;
                    oldOgImageUrl = existingSetting.OgImage;

                    existingSetting.Title = settingHome.Title;
                    existingSetting.ImageUrl = settingHome.ImageUrl;
                    existingSetting.Link = settingHome.Link;
                    existingSetting.MetaTitle = settingHome.MetaTitle;
                    existingSetting.MetaDescription = settingHome.MetaDescription;
                    existingSetting.MetaKeywords = settingHome.MetaKeywords;
                    existingSetting.OgTitle = settingHome.OgTitle;
                    existingSetting.OgDescription = settingHome.OgDescription;
                    existingSetting.OgImage = settingHome.OgImage;
                    existingSetting.OgUrl = settingHome.OgUrl;
                    existingSetting.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                // Clean up old images if they were replaced
                await TryRemoveOldImage(oldImageUrl, settingHome.ImageUrl);
                await TryRemoveOldImage(oldOgImageUrl, settingHome.OgImage);

                return Ok(new { success = true, message = "Settings saved successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error saving settings: {ex.Message}" });
            }
        }

        private async Task TryRemoveOldImage(string oldUrl, string newUrl)
        {
            if (!string.IsNullOrEmpty(oldUrl) && oldUrl != newUrl)
            {
                try
                {
                    var fileName = Path.GetFileName(oldUrl);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        var imagePath = Path.Combine(_environment.WebRootPath, "images-home", fileName);
                        if (System.IO.File.Exists(imagePath))
                        {
                            await Task.Run(() => System.IO.File.Delete(imagePath));
                        }
                    }
                }
                catch
                {
                    // Silent failure - not critical
                }
            }
        }
    }
}