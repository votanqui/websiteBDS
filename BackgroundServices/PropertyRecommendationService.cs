using Microsoft.EntityFrameworkCore;
using thuctap2025.Data;
using thuctap2025.Models;
using thuctap2025.Services;

namespace thuctap2025.BackgroundServices
{
    public class PropertyRecommendationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PropertyRecommendationService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromDays(1); // Chạy mỗi ngày 1 lần

        public PropertyRecommendationService(
            IServiceProvider serviceProvider,
            ILogger<PropertyRecommendationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Đợi 1 phút sau khi khởi động để tránh load cao lúc startup
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPropertyRecommendationsAsync();
                    _logger.LogInformation($"Property recommendation check completed at {DateTime.Now}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing property recommendations");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task ProcessPropertyRecommendationsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            // Lấy các user đã có hoạt động xem property trong 7 ngày qua
            var sevenDaysAgo = DateTime.Now.AddDays(-7);
            var activeUsers = await dbContext.PropertyViews
                .Include(pv => pv.User)
                .Include(pv => pv.Property)
                    .ThenInclude(p => p.PropertyCategoryMappings)
                .Where(pv => pv.ViewedAt >= sevenDaysAgo && pv.User != null)
                .GroupBy(pv => pv.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    User = g.First().User,
                    ViewedProperties = g.Select(pv => pv.Property).ToList(),
                    LastViewedAt = g.Max(pv => pv.ViewedAt)
                })
                .ToListAsync();

            foreach (var userActivity in activeUsers)
            {
                try
                {
                    if (userActivity.User?.Email != null)
                    {
                        var recommendations = await GetRecommendationsForUser(
                            dbContext,
                            userActivity.UserId.Value,
                            userActivity.ViewedProperties);

                        if (recommendations.Any())
                        {
                            await emailService.SendPropertyRecommendationsAsync(
                                userActivity.User.Email,
                                userActivity.User.FullName ?? userActivity.User.UserName ?? "Khách hàng",
                                recommendations);

                            _logger.LogInformation($"Recommendation email sent to user {userActivity.User.UserName}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to process recommendations for user {userActivity.UserId}");
                }
            }
        }

        private async Task<List<Property>> GetRecommendationsForUser(
            ApplicationDbContext dbContext,
            int userId,
            List<Property> viewedProperties)
        {
            var recommendations = new List<Property>();

            // Lấy các property đã xem để loại trừ
            var viewedPropertyIds = viewedProperties.Select(p => p.Id).ToList();
            var favoritePropertyIds = await dbContext.Favorites
                .Where(f => f.UserId == userId)
                .Select(f => f.PropertyId)
                .ToListAsync();

            var excludedIds = viewedPropertyIds.Concat(favoritePropertyIds).Distinct().ToList();

            foreach (var viewedProperty in viewedProperties.Take(3)) // Chỉ xử lý 3 property gần đây nhất
            {
                var similarProperties = await FindSimilarProperties(
                    dbContext,
                    viewedProperty,
                    excludedIds);

                recommendations.AddRange(similarProperties);
            }

            // Loại bỏ trùng lặp và lấy tối đa 10 gợi ý
            return recommendations
                .GroupBy(p => p.Id)
                .Select(g => g.First())
                .OrderByDescending(p => p.IsVip)
                .ThenByDescending(p => p.CreatedAt)
                .Take(10)
                .ToList();
        }

        private async Task<List<Property>> FindSimilarProperties(
            ApplicationDbContext dbContext,
            Property referenceProperty,
            List<int> excludedIds)
        {
            var similarProperties = new List<Property>();

            // 1. Tìm property cùng danh mục
            var sameCategoryProperties = await FindPropertiesBySameCategory(
                dbContext, referenceProperty, excludedIds);
            similarProperties.AddRange(sameCategoryProperties);

            // 2. Tìm property theo giá tương tự
            var similarPriceProperties = await FindPropertiesBySimilarPrice(
                dbContext, referenceProperty, excludedIds);
            similarProperties.AddRange(similarPriceProperties);

            // 3. Tìm property theo vị trí gần
            var nearbyProperties = await FindPropertiesByLocation(
                dbContext, referenceProperty, excludedIds);
            similarProperties.AddRange(nearbyProperties);

            return similarProperties;
        }

        private async Task<List<Property>> FindPropertiesBySameCategory(
            ApplicationDbContext dbContext,
            Property referenceProperty,
            List<int> excludedIds)
        {
            var categoryIds = await dbContext.PropertyCategoryMappings
                .Where(pcm => pcm.PropertyId == referenceProperty.Id)
                .Select(pcm => pcm.CategoryId)
                .ToListAsync();

            if (!categoryIds.Any()) return new List<Property>();

            return await dbContext.Properties
                .Include(p => p.User)
                .Include(p => p.PropertyImages)
                .Include(p => p.PropertyCategoryMappings)
                .Where(p => p.Status == "Approved" &&
                           !excludedIds.Contains(p.Id) &&
                           p.PropertyCategoryMappings.Any(pcm => categoryIds.Contains(pcm.CategoryId)))
                .OrderByDescending(p => p.IsVip)
                .ThenByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync();
        }

        private async Task<List<Property>> FindPropertiesBySimilarPrice(
            ApplicationDbContext dbContext,
            Property referenceProperty,
            List<int> excludedIds)
        {
            if (!referenceProperty.Price.HasValue) return new List<Property>();

            var referencePrice = referenceProperty.Price.Value;
            var priceRangeMin = referencePrice * 0.7m; // -30%
            var priceRangeMax = referencePrice * 1.3m; // +30%

            return await dbContext.Properties
                .Include(p => p.User)
                .Include(p => p.PropertyImages)
                .Include(p => p.PropertyCategoryMappings)
                .Where(p => p.Status == "Approved" &&
                           !excludedIds.Contains(p.Id) &&
                           p.Price.HasValue &&
                           p.Price >= priceRangeMin &&
                           p.Price <= priceRangeMax)
                .OrderByDescending(p => p.IsVip)
                .ThenByDescending(p => p.CreatedAt)
                .Take(3)
                .ToListAsync();
        }

        private async Task<List<Property>> FindPropertiesByLocation(
            ApplicationDbContext dbContext,
            Property referenceProperty,
            List<int> excludedIds)
        {
            if (!referenceProperty.Latitude.HasValue || !referenceProperty.Longitude.HasValue)
                return new List<Property>();

            var refLat = referenceProperty.Latitude.Value;
            var refLng = referenceProperty.Longitude.Value;
            var radiusKm = 5.0; // Bán kính 5km

            // Tính toán khoảng cách đơn giản (không chính xác 100% nhưng đủ dùng)
            var latRange = radiusKm / 111.0; // 1 độ latitude ≈ 111km
            var lngRange = radiusKm / (111.0 * Math.Cos(refLat * Math.PI / 180.0));

            var minLat = refLat - latRange;
            var maxLat = refLat + latRange;
            var minLng = refLng - lngRange;
            var maxLng = refLng + lngRange;

            return await dbContext.Properties
                .Include(p => p.User)
                .Include(p => p.PropertyImages)
                .Include(p => p.PropertyCategoryMappings)
                .Where(p => p.Status == "Approved" &&
                           !excludedIds.Contains(p.Id) &&
                           p.Latitude.HasValue &&
                           p.Longitude.HasValue &&
                           p.Latitude >= minLat &&
                           p.Latitude <= maxLat &&
                           p.Longitude >= minLng &&
                           p.Longitude <= maxLng)
                .OrderByDescending(p => p.IsVip)
                .ThenByDescending(p => p.CreatedAt)
                .Take(3)
                .ToListAsync();
        }
    }
}