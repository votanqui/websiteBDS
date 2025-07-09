using thuctap2025.BackgroundServices;
using thuctap2025.Services;

namespace thuctap2025.Extensions
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddEmailNotificationServices(this IServiceCollection services)
        {
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IPasswordResetService, PasswordResetService>();
            services.AddHostedService<UnreadMessageNotificationService>();
            services.AddHostedService<VipNotificationService>();
            services.AddHostedService<SuspiciousLoginMonitorService>();
            services.AddHostedService<PropertyRecommendationService>();
            return services;
        }
    }
}
