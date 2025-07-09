namespace thuctap2025.DTOs
{
    public class CreateNewsRequest
    {
        public string Title { get; set; }
        public string Slug { get; set; }
        public string ShortDescription { get; set; }
        public string Content { get; set; }
        public int CategoryId { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string MetaKeywords { get; set; }

        public List<string> ImageUrls { get; set; } = new();
        public string FeaturedImage { get; set; }

        public List<int> TagIds { get; set; } = new();
    }

}
