using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using thuctap2025.Data;
using thuctap2025.DTOs;

namespace thuctap2025.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AuditLogService _auditLogService;
        public StatisticsController(ApplicationDbContext context, AuditLogService auditLogService)
        {
            _context = context;
            _auditLogService = auditLogService;
        }
        [HttpGet("summary")]
        public async Task<ActionResult<AdminSummaryStatistics>> GetSummaryStatistics()
        {
            var statistics = new AdminSummaryStatistics
            {
                TotalUsers = await _context.Users.CountAsync(),
                NewUsersThisMonth = await _context.Users
                    .Where(u => u.CreatedAt >= DateTime.Now.AddMonths(-1))
                    .CountAsync(),
                TotalProperties = await _context.Properties.CountAsync(),
                ActiveProperties = await _context.Properties
                    .Where(p => p.Status == "Approved")
                    .CountAsync(),
                VIPProperties = await _context.Properties
                    .Where(p => p.IsVip && p.VipEndDate >= DateTime.Now)
                    .CountAsync(),
                TotalNews = await _context.News.CountAsync(),
                PublishedNews = await _context.News
                    .Where(n => n.IsPublished)
                    .CountAsync(),
                TotalFavorites = await _context.Favorites.CountAsync(),
                TotalMessages = await _context.ChatMessage.CountAsync(),
                PendingApprovals = await _context.Properties
                    .Where(p => p.Status == "Pending")
                    .CountAsync() +
                    await _context.Users
                    .Where(u => u.AccountStatus == "Pending")
                    .CountAsync()

            };

            return Ok(statistics);
        }
        // GET /api/admin/statistics/users
        [HttpGet("users")]
        public async Task<ActionResult<UserStatistics>> GetUserStatistics(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var query = _context.Users.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(u => u.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(u => u.CreatedAt <= endDate.Value);

            var statistics = new UserStatistics
            {
                TotalUsers = await query.CountAsync(),
                UsersByStatus = await query
                    .GroupBy(u => u.AccountStatus)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status, x => x.Count),
                UsersByRole = await query
                    .GroupBy(u => u.Role)
                    .Select(g => new { Role = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Role, x => x.Count),
                RegistrationTrend = await query
                    .Where(u => u.CreatedAt >= DateTime.Now.AddMonths(-6))
                    .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                    .OrderBy(g => g.Key.Year)
                    .ThenBy(g => g.Key.Month)
                    .Select(g => new {
                        Period = $"{g.Key.Month}/{g.Key.Year}",
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(x => x.Period, x => x.Count)
            };

            return Ok(statistics);
        }

        // GET /api/admin/statistics/properties
        [HttpGet("properties")]
        public async Task<ActionResult<PropertyStatistics>> GetPropertyStatistics(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var query = _context.Properties.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(p => p.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(p => p.CreatedAt <= endDate.Value);

            var statistics = new PropertyStatistics
            {
                TotalProperties = await query.CountAsync(),
                PropertiesByStatus = await query
                    .GroupBy(p => p.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status, x => x.Count),
                VIPPropertiesCount = await query
                    .Where(p => p.IsVip && p.VipEndDate >= DateTime.Now)
                    .CountAsync(),
                AveragePrice = await query
                    .Where(p => p.Price.HasValue)
                    .AverageAsync(p => p.Price.Value),
                PropertiesTrend = await query
                    .Where(p => p.CreatedAt >= DateTime.Now.AddMonths(-6))
                    .GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
                    .OrderBy(g => g.Key.Year)
                    .ThenBy(g => g.Key.Month)
                    .Select(g => new {
                        Period = $"{g.Key.Month}/{g.Key.Year}",
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(x => x.Period, x => x.Count),
                TopViewedProperties = await _context.PropertyViews
                    .Include(pv => pv.Property)
                    .GroupBy(pv => pv.PropertyId)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .Select(g => new {
                        PropertyId = g.Key,
                        Title = g.First().Property.Title,
                        ViewCount = g.Count()
                    })
                    .ToListAsync()
            };

            return Ok(statistics);
        }
        // GET /api/admin/statistics/news
        [HttpGet("news")]
        public async Task<ActionResult<NewsStatistics>> GetNewsStatistics(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var query = _context.News.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(n => n.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(n => n.CreatedAt <= endDate.Value);

            var statistics = new NewsStatistics
            {
                TotalNews = await query.CountAsync(),
                PublishedNewsCount = await query
                    .Where(n => n.IsPublished)
                    .CountAsync(),
                NewsByCategory = await query
                    .Include(n => n.Category)
                    .GroupBy(n => n.Category.Name)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Category, x => x.Count),
                TopViewedNews = await query
                    .OrderByDescending(n => n.ViewCount)
                    .Take(10)
                    .Select(n => new {
                        n.Id,
                        n.Title,
                        n.ViewCount
                    })
                    .ToListAsync(),
                NewsTrend = await query
                    .Where(n => n.CreatedAt >= DateTime.Now.AddMonths(-6))
                    .GroupBy(n => new { n.CreatedAt.Year, n.CreatedAt.Month })
                    .OrderBy(g => g.Key.Year)
                    .ThenBy(g => g.Key.Month)
                    .Select(g => new {
                        Period = $"{g.Key.Month}/{g.Key.Year}",
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(x => x.Period, x => x.Count)
            };

            return Ok(statistics);
        }
        // GET /api/admin/statistics/activities
        [HttpGet("activities")]
        public async Task<ActionResult<ActivityStatistics>> GetActivityStatistics(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var query = _context.PropertyViews.AsQueryable();
            var favoritesQuery = _context.Favorites.AsQueryable();
            var messagesQuery = _context.ChatMessage.AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(pv => pv.ViewedAt >= startDate.Value);
                favoritesQuery = favoritesQuery.Where(f => f.CreatedAt >= startDate.Value);
                messagesQuery = messagesQuery.Where(m => m.SentAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(pv => pv.ViewedAt <= endDate.Value);
                favoritesQuery = favoritesQuery.Where(f => f.CreatedAt <= endDate.Value);
                messagesQuery = messagesQuery.Where(m => m.SentAt <= endDate.Value);
            }

            var statistics = new ActivityStatistics
            {
                TotalViews = await query.CountAsync(),
                TotalFavorites = await favoritesQuery.CountAsync(),
                TotalMessages = await messagesQuery.CountAsync(),
                ViewsTrend = await query
                    .Where(pv => pv.ViewedAt >= DateTime.Now.AddMonths(-6))
                    .GroupBy(pv => new { pv.ViewedAt.Year, pv.ViewedAt.Month })
                    .OrderBy(g => g.Key.Year)
                    .ThenBy(g => g.Key.Month)
                    .Select(g => new {
                        Period = $"{g.Key.Month}/{g.Key.Year}",
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(x => x.Period, x => x.Count),
                MostActiveUsers = await _context.Users
                    .Select(u => new {
                        u.Id,
                        u.UserName,
                        u.FullName,
                        ViewCount = u.Properties.SelectMany(p => p.PropertyViews).Count(),
                        FavoriteCount = u.Favorites.Count,
                        MessageCount = _context.ChatMessage
                            .Count(m => m.SenderId == u.Id.ToString() || m.ReceiverId == u.Id.ToString())
                    })
                    .OrderByDescending(u => u.ViewCount + u.FavoriteCount + u.MessageCount)
                    .Take(10)
                    .ToListAsync()
            };

            return Ok(statistics);
        }
      
        [HttpGet("auditlog")]
        public async Task<ActionResult<AuditLogSummaryStatistics>> GetAuditLogSummaryStatistics(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(log => log.ActionTime >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(log => log.ActionTime <= endDate.Value);

            var statistics = new AuditLogSummaryStatistics
            {
                TotalLogs = await query.CountAsync(),
                LogsByActionType = await query
                    .GroupBy(log => log.ActionType)
                    .Select(g => new { ActionType = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ActionType, x => x.Count),
                LogsByTable = await query
                    .GroupBy(log => log.TableName)
                    .Select(g => new { TableName = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.TableName, x => x.Count),
                RecentActivities = await query
                    .OrderByDescending(log => log.ActionTime)
                    .Take(10)
                    .Select(log => new {
                        log.Id,
                        log.ActionType,
                        log.TableName,
                        log.RecordId,
                        UserName = _context.Users
                            .Where(u => u.Id == log.UserId)
                            .Select(u => u.UserName)
                            .FirstOrDefault() ?? "System",
                        log.ActionTime
                    })
                    .ToListAsync()

            };

            return Ok(statistics);
        }


    }
}
