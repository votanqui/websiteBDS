using thuctap2025.DTOs;
using thuctap2025.Models;

namespace thuctap2025.Services
{
    public interface IEmailService
    {
        Task SendUnreadMessagesNotificationAsync(string userEmail, string userName, List<ChatMessageWithSender> unreadMessages);
        Task SendPasswordResetEmailAsync(string userEmail, string userName, string resetToken);

        Task SendEmailConfirmationAsync(string userEmail, string userName, string confirmationToken);



        Task SendVipExpiredNotificationAsync(string userEmail, string userName, Property property);
        Task SendVipExpiringNotificationAsync(string userEmail, string userName, Property property, int daysLeft);






        Task SendSuspiciousActivityNotificationAsync(string userEmail, string userName, string suspiciousIP, DateTime lastLoginTime);
        Task SendAdminSuspiciousActivityReportAsync(string adminEmail, object suspiciousActivityReport);



        Task SendPropertyRecommendationsAsync(string userEmail, string userName, List<Property> recommendations);
    }
}
