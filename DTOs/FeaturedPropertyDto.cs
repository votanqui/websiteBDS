namespace thuctap2025.DTOs
{
    public class FeaturedPropertyDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public decimal? Price { get; set; } = null!;
        public string Location { get; set; } = null!;
        public int? Beds { get; set; }
        public int? Baths { get; set; }
        public double? Area { get; set; } = null!;
        public string Image { get; set; } = null!;
        public bool IsVip { get; set; }
    }

}
