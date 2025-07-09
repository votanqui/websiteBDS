namespace thuctap2025.Models
{
    public class NewsImage
    {
        public int Id { get; set; }
        public int NewsId { get; set; }
        public string ImageUrl { get; set; }
        public bool IsFeatured { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }

        public News News { get; set; }
    }

}
