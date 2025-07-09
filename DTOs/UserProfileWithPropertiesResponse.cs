namespace thuctap2025.DTOs
{
    public class UserProfileWithPropertiesResponse
    {
        public UserProfileDto UserInfo { get; set; }
        public List<PropertySummaryDto> Properties { get; set; } = new();
        public PaginationDto Pagination { get; set; }
    }
}
