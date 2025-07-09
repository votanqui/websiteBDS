namespace thuctap2025.Models
{
    public class NewsTagMapping
    {
        public int Id { get; set; }
        public int NewsId { get; set; }
        public int TagId { get; set; }

        public News News { get; set; }
        public NewsTag Tag { get; set; }
    }

}
