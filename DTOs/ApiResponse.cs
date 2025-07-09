namespace thuctap2025.DTOs
{
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public object? Data { get; set; }
    }
}
