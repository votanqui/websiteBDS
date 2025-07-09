namespace thuctap2025.DTOs
{
    public class CreatePropertyRequest
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public decimal Price { get; set; }
        public float Area { get; set; }
        public int Bedrooms { get; set; }
        public int Bathrooms { get; set; }
        public int UserId { get; set; }  // Giả định đang login và biết UserId

        public float Latitude { get; set; }
        public float Longitude { get; set; }

        public List<int> CategoryIds { get; set; }             // Danh sách ID của category
        public List<FeatureDto> Features { get; set; }         // Danh sách feature name + value
        public List<string> ImageUrls { get; set; }            // Danh sách ảnh

        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string MetaKeywords { get; set; }
        public string CanonicalUrl { get; set; }

        public string MainImage { get; set; } // URL ảnh chính
    }

    public class FeatureDto
    {
        public string FeatureName { get; set; }
        public string FeatureValue { get; set; }
    }

}
