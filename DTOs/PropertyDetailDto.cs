namespace thuctap2025.DTOs
{
    public class PropertyDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public decimal? Price { get; set; }
        public double? Area { get; set; }
        public int? Bedrooms { get; set; }
        public int? Bathrooms { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserName { get; set; }
        public string phone { get; set; }
        public int UserId { get; set; }
        public List<string> Categories { get; set; }
        public List<string> Features { get; set; }
        public List<string> Images { get; set; }
        public string MainImage { get; set; }
        public bool IsVip { get; set; }
        public string? AvatarUrl { get; set; }


        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string MetaKeywords { get; set; }
        public string CanonicalUrl { get; set; }
    }

}
