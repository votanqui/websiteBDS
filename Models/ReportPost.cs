namespace thuctap2025.Models
{
    public class ReportPost
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public int UserId { get; set; }
        public string Reason { get; set; }
        public string Note { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Property Property { get; set; }
        public Users User { get; set; }
    }

}
