using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using thuctap2025.Data;
using thuctap2025.Models;
using System.IO;
using System.Threading.Tasks;

namespace thuctap2025.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class BankSettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public BankSettingsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }
        // GET: api/BankSettings/transfer-info
        [HttpGet("transfer-info")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTransferInfo()
        {
            var activeSetting = await _context.BankSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.IsActive);

            if (activeSetting == null)
            {
                return NotFound(new { success = false, message = "Không có thông tin chuyển khoản nào được cấu hình." });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    bankName = activeSetting.BankName,
                    accountNumber = activeSetting.AccountNumber,
                    accountHolder = activeSetting.AccountHolder,
                    imageQR = activeSetting.ImageQR
                }
            });
        }

        // GET: api/BankSettings/default
        [HttpGet("default")]
        [AllowAnonymous]
        public async Task<ActionResult<BankSetting>> GetDefaultBankSetting()
        {
            var setting = await _context.BankSettings.FirstOrDefaultAsync(b => b.IsActive);
            return setting ?? new BankSetting();
        }

        // POST: api/BankSettings/default
        [HttpPost("default")]
        public async Task<IActionResult> SaveDefaultBankSetting(BankSetting setting)
        {
            try
            {
                var existing = await _context.BankSettings.FirstOrDefaultAsync(b => b.IsActive);
                string oldQr = existing?.ImageQR;

                if (existing == null)
                {
                    setting.CreatedAt = DateTime.Now;
                    _context.BankSettings.Add(setting);
                }
                else
                {
                    existing.BankName = setting.BankName;
                    existing.AccountNumber = setting.AccountNumber;
                    existing.AccountHolder = setting.AccountHolder;
                    existing.ImageQR = setting.ImageQR;
                    existing.IsActive = setting.IsActive;
                    existing.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                // Remove old QR if it was replaced with a new one
                if (!string.IsNullOrEmpty(oldQr) && oldQr != setting.ImageQR)
                {
                    await RemoveQRFile(oldQr);
                }

                return Ok(new { success = true, message = "Bank setting saved successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        public class UploadQRRequest
        {
            public IFormFile File { get; set; }
        }

        // POST: api/BankSettings/UploadQR
        [HttpPost("UploadQR")]
        [Authorize]
        public async Task<IActionResult> UploadQR([FromForm] UploadQRRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest(new { success = false, message = "No file uploaded" });

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "bank-qr");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest(new { success = false, message = "Invalid file type" });

            // Get current active bank setting to check for existing QR
            var currentSetting = await _context.BankSettings.FirstOrDefaultAsync(b => b.IsActive);
            string oldQrPath = currentSetting?.ImageQR;

            // Generate new file name and save
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream);
            }

            var newUrl = $"/bank-qr/{uniqueFileName}";

            // Remove old QR file if it exists
            if (!string.IsNullOrEmpty(oldQrPath))
            {
                await RemoveQRFile(oldQrPath);
            }

            return Ok(new { success = true, url = newUrl });
        }

        // DELETE: api/BankSettings/RemoveQR?imageUrl=/bank-qr/abc.png
        [HttpDelete("RemoveQR")]
        public async Task<IActionResult> RemoveQR([FromQuery] string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return BadRequest(new { success = false, message = "Image URL is required" });

            try
            {
                await RemoveQRFile(imageUrl);
                return Ok(new { success = true, message = "QR image deleted" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        private async Task RemoveQRFile(string imageUrl)
        {
            var fileName = Path.GetFileName(imageUrl);
            if (!string.IsNullOrEmpty(fileName))
            {
                var path = Path.Combine(_environment.WebRootPath, "bank-qr", fileName);
                if (System.IO.File.Exists(path))
                {
                    await Task.Run(() => System.IO.File.Delete(path));
                }
            }
        }
    }
}