namespace thuctap2025.DTOs
{
    public class UserStatistics
    {
        public int TotalUsers { get; set; }
        public Dictionary<string, int> UsersByStatus { get; set; }
        public Dictionary<string, int> UsersByRole { get; set; }
        public Dictionary<string, int> RegistrationTrend { get; set; }
    }
}
