namespace thuctap2025.DTOs
{
    public class PropertySummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public decimal? Price { get; set; }
        public double? Area { get; set; }
        public string? Address { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsVip { get; set; }
        public int? Bedrooms { get; set; }
        public int? Bathrooms { get; set; }

        public string? MainImageUrl { get; set; }
        public List<string> Categories { get; set; } = new();
        public int ViewCount { get; set; }
 
    }
}
