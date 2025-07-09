namespace thuctap2025.Models
{
    public class PropertyCategoryMapping
    {
        public int Id { get; set; }

        public int PropertyId { get; set; }
        public Property Property { get; set; }

        public int CategoryId { get; set; }
        public PropertyCategory Category { get; set; }
    }

}
