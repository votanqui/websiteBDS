namespace thuctap2025.DTOs
{
    public class ActivityStatistics
    {
        public int TotalViews { get; set; }
        public int TotalFavorites { get; set; }
        public int TotalMessages { get; set; }
        public Dictionary<string, int> ViewsTrend { get; set; }
        public dynamic MostActiveUsers { get; set; }
    }

}
