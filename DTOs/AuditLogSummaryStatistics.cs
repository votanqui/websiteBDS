namespace thuctap2025.DTOs
{
    public class AuditLogSummaryStatistics
    {
        public int TotalLogs { get; set; }
        public Dictionary<string, int> LogsByActionType { get; set; }
        public Dictionary<string, int> LogsByTable { get; set; }
        public dynamic RecentActivities { get; set; }
    }
}
