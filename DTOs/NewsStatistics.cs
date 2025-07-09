namespace thuctap2025.DTOs
{
    public class NewsStatistics
    {
        public int TotalNews { get; set; }
        public int PublishedNewsCount { get; set; }
        public Dictionary<string, int> NewsByCategory { get; set; }
        public dynamic TopViewedNews { get; set; }
        public Dictionary<string, int> NewsTrend { get; set; }
    }
}
