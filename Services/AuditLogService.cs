// Services/AuditLogService.cs
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using thuctap2025.Data;
using thuctap2025.Models;

public class AuditLogService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogActionAsync(string actionType, string tableName, int? recordId,
                                   string? oldValues = null, string? newValues = null)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        var userAgent = _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString();

        var log = new AuditLog
        {
            ActionType = actionType,
            TableName = tableName,
            RecordId = recordId,
            UserId = userId != null ? int.Parse(userId) : null,
            IPAddress = ipAddress,
            UserAgent = userAgent,
            OldValues = oldValues,
            NewValues = newValues,
            ActionTime = DateTime.Now
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}