namespace thuctap2025.Models
{
    public class Favorite
    {
        public int Id { get; set; }

        public int PropertyId { get; set; }
        public Property Property { get; set; }

        public int UserId { get; set; }
        public Users User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

}
