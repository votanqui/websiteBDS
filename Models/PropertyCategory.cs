namespace thuctap2025.Models
{
    public class PropertyCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<PropertyCategoryMapping> PropertyCategoryMappings { get; set; }
    }

}
