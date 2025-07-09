namespace thuctap2025.DTOs
{
    public class PropertyStatistics
    {
        public int TotalProperties { get; set; }
        public Dictionary<string, int> PropertiesByStatus { get; set; }
        public int VIPPropertiesCount { get; set; }
        public decimal AveragePrice { get; set; }
        public Dictionary<string, int> PropertiesTrend { get; set; }
        public dynamic TopViewedProperties { get; set; }
    }
}
