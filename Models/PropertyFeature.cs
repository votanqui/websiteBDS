namespace thuctap2025.Models
{
    public class PropertyFeature
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public Property Property { get; set; }

        public string FeatureName { get; set; } = null!;
        public string? FeatureValue { get; set; }
    }

}
