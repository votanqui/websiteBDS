namespace thuctap2025.Models
{
    public class PropertyImage
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public Property Property { get; set; }

        public string ImageUrl { get; set; } = null!;
        public string? Caption { get; set; }
        public bool IsPrimary { get; set; } = false;
        public int SortOrder { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

}
