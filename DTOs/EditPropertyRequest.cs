namespace thuctap2025.DTOs
{
    public class EditPropertyRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public decimal? Price { get; set; }
        public double? Area { get; set; }
        public int? Bedrooms { get; set; }
        public int? Bathrooms { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public List<int> CategoryIds { get; set; }
        public List<FeatureDto2> Features { get; set; }
        public List<string> ImageUrls { get; set; }
        public string MainImage { get; set; }

        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string MetaKeywords { get; set; }
        public string CanonicalUrl { get; set; }
    }

    public class FeatureDto2
    {
        public string FeatureName { get; set; }
        public string FeatureValue { get; set; }
    }

}
