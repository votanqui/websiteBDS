using System.Security.Cryptography.Xml;

namespace thuctap2025.Models
{
    public class Property
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public string? Address { get; set; }
        public decimal? Price { get; set; }
        public double? Area { get; set; }  // Đổi từ float? sang double?
        public int? Bedrooms { get; set; }
        public int? Bathrooms { get; set; }
        public string Status { get; set; } = "Pending";
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public double? Latitude { get; set; }  // Đổi từ float? sang double?
        public double? Longitude { get; set; } // Đổi từ float? sang double?

        public bool IsVip { get; set; } = false;
        public DateTime? VipStartDate { get; set; }
        public DateTime? VipEndDate { get; set; }

        public Users User { get; set; }
        public SeoInfo SeoInfo { get; set; }
        public ICollection<PropertyImage> PropertyImages { get; set; }
        public ICollection<PropertyCategoryMapping> PropertyCategoryMappings { get; set; }
        public ICollection<PropertyFeature> PropertyFeatures { get; set; }
        public ICollection<Favorite> Favorites { get; set; }
        public virtual ICollection<PropertyView> PropertyViews { get; set; }
    }


}
