namespace thuctap2025.Models
{
    public class News
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string ShortDescription { get; set; }
        public string Content { get; set; }

        public int AuthorId { get; set; }
        public int? CategoryId { get; set; }

        public int ViewCount { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }

        // SEO Fields
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string MetaKeywords { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public Users Author { get; set; }
        public NewsCategory Category { get; set; }

        public ICollection<NewsImage> NewsImages { get; set; } = new List<NewsImage>();
        public ICollection<NewsTagMapping> NewsTagMappings { get; set; } = new List<NewsTagMapping>();
    }

}
