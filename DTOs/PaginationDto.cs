namespace thuctap2025.DTOs
{
    public class PaginationDto
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalProperties { get; set; }
        public int TotalPages { get; set; }
    }
}
