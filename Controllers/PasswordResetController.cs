using Microsoft.AspNetCore.Mvc;
using thuctap2025.DTOs;
using thuctap2025.Services;

namespace thuctap2025.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PasswordResetController : ControllerBase
    {
        private readonly IPasswordResetService _passwordResetService;
        private readonly ILogger<PasswordResetController> _logger;

        public PasswordResetController(
            IPasswordResetService passwordResetService,
            ILogger<PasswordResetController> logger)
        {
            _passwordResetService = passwordResetService;
            _logger = logger;
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ",
                    Data = ModelState
                });
            }

            var result = await _passwordResetService.SendPasswordResetEmailAsync(request.Email);

            if (result.Success)
                return Ok(result);
            else
                return BadRequest(result);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Dữ liệu không hợp lệ",
                    Data = ModelState
                });
            }

            var result = await _passwordResetService.ResetPasswordAsync(request);

            if (result.Success)
                return Ok(result);
            else
                return BadRequest(result);
        }

        [HttpGet("validate-token")]
        public async Task<IActionResult> ValidateToken([FromQuery] string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest(new ApiResponse
                {
                    Success = false,
                    Message = "Token không được để trống"
                });
            }

            var result = await _passwordResetService.ValidateResetTokenAsync(token);

            if (result.Success)
                return Ok(result);
            else
                return BadRequest(result);
        }
    }
}