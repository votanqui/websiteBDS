using thuctap2025.DTOs;

namespace thuctap2025.Services
{
    public interface IPasswordResetService
    {
        Task<ApiResponse> SendPasswordResetEmailAsync(string email);
        Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest request);
        Task<ApiResponse> ValidateResetTokenAsync(string token);
    }
}
