namespace thuctap2025.DTOs
{
    public class PropertyCategoryDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
