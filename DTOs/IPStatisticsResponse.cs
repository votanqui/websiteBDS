namespace thuctap2025.DTOs
{
    public class IPStatisticsResponse
    {
        public string IPAddress { get; set; } = null!;
        public int LoginCount { get; set; }
        public int UniqueUsers { get; set; }
        public DateTime LastLogin { get; set; }
        public DateTime FirstLogin { get; set; }
    }
}
