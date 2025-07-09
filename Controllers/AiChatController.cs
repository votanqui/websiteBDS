using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using thuctap2025.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace thuctap2025.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AiChatController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public AiChatController(ApplicationDbContext context, IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }
        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            var question = request.Question?.ToLower() ?? "";

            // Các từ khóa liên quan đến bất động sản
            var keywords = new[]
            {
        "bất động sản", "căn hộ", "villa", "biệt thự", "mặt bằng", "đất", "thửa đất", "cho thuê", "bán", "mua", "thuê", "vị trí",
        "quận", "huyện", "phường", "tỉnh", "khu vực", "khu đô thị", "khu chung cư", "phòng ngủ", "phòng tắm", "diện tích", "giá", "giá thuê",
        "nội thất", "chung cư", "phòng trọ", "nhà nguyên căn", "sổ đỏ", "vay mua nhà", "pháp lý", "xây dựng", "thị trường", "dự án"
    };

            var phraseKeywords = keywords.Where(k => k.Contains(' '));
            var wordKeywords = keywords.Where(k => !k.Contains(' ') && k.Length > 3);

            bool phraseMatch = phraseKeywords.Any(k => question.Contains(k));
            bool wordMatch = wordKeywords.Any(k => Regex.IsMatch(question, $@"\b{k}\b", RegexOptions.IgnoreCase));
            bool isRealEstateRelated = phraseMatch || wordMatch;

            // Tạo HttpClient dùng chung
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config["OpenAI:ApiKey"]);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            string systemPrompt = "";
            string userPrompt = "";

            if (isRealEstateRelated)
            {
                // 1. Truy vấn toàn bộ bất động sản
                var allProperties = _context.Properties
                    .Include(p => p.User)
                    .Include(p => p.PropertyImages)
                    .Select(p => new
                    {
                        p.Id,
                        p.Title,
                        p.Address,
                        p.Price,
                        p.Bedrooms,
                        p.Bathrooms,
                        p.Area,
                        UserName = p.User.FullName,
                        ImageUrl = p.PropertyImages
                            .Where(img => img.IsPrimary)
                            .OrderBy(img => img.SortOrder)
                            .Select(img => img.ImageUrl)
                            .FirstOrDefault()
                    })
                    .ToList();

                // 2. Gửi prompt trích lọc tiêu chí bất động sản từ câu hỏi
                var criteriaMessages = new[]
                {
            new { role = "system", content = "Bạn là trợ lý chuyên phân tích nhu cầu tìm kiếm bất động sản. Hãy trích xuất tiêu chí tìm kiếm từ câu hỏi dưới dạng JSON như: { \"maxPrice\": ..., \"minArea\": ..., \"bedrooms\": ..., \"location\": \"...\" }. Nếu không rõ, có thể bỏ qua trường đó." },
            new { role = "user", content = request.Question }
        };

                var criteriaPayload = new
                {
                    model = "gpt-4o-mini",
                    messages = criteriaMessages,
                    temperature = 0.3
                };

                var criteriaContent = new StringContent(JsonSerializer.Serialize(criteriaPayload), Encoding.UTF8, "application/json");
                var criteriaResponse = await client.PostAsync("https://api.openai.com/v1/chat/completions", criteriaContent);
                if (!criteriaResponse.IsSuccessStatusCode)
                {
                    var err = await criteriaResponse.Content.ReadAsStringAsync();
                    return BadRequest(new { error = "GPT lỗi (phân tích tiêu chí)", detail = err });
                }

                var criteriaResult = await criteriaResponse.Content.ReadFromJsonAsync<OpenAiResponse>();
                var criteriaText = criteriaResult?.Choices?.FirstOrDefault()?.Message?.Content;

                // Parse JSON kết quả tiêu chí
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var criteria = JsonSerializer.Deserialize<RealEstateCriteria>(criteriaText ?? "{}", options);

                // 3. Lọc top 5 bất động sản phù hợp nhất
                var filtered = allProperties
                    .Where(p =>
                        (criteria.MaxPrice == null || p.Price <= criteria.MaxPrice) &&
                        (criteria.MinArea == null || p.Area >= criteria.MinArea) &&
                        (criteria.Bedrooms == null || p.Bedrooms >= criteria.Bedrooms) &&
                        (string.IsNullOrEmpty(criteria.Location) || p.Address.Contains(criteria.Location, StringComparison.OrdinalIgnoreCase))
                    )
                    .Take(5)
                    .ToList();

                var baseUrl = "https://localhost:7054";

                var propertyText = string.Join("\n\n", filtered.Select(p =>
                 $"**ID Bất động sản:** {p.Id}\n" +
                    $"### {p.Title}\n" +
                    $"![Ảnh bất động sản]({(p.ImageUrl != null ? baseUrl + p.ImageUrl : baseUrl + "/images/default-property.jpg")})\n" +
                    $"**Địa chỉ:** {p.Address}\n" +
                    $"**Giá:** {p.Price:N0} VND\n" +
                    $"**Diện tích:** {p.Area} m²\n" +
                    $"**Phòng ngủ:** {p.Bedrooms}\n" +
                    $"**Phòng tắm:** {p.Bathrooms}\n" +
                    $"**Chủ sở hữu:** {p.UserName}"
                ));

                systemPrompt = @"Bạn là trợ lý bất động sản chuyên nghiệp. Hãy trả lời câu hỏi dựa trên thông tin bất động sản được cung cấp.
- Sử dụng markdown để định dạng
- Luôn hiển thị ảnh bất động sản nếu có
- Định dạng giá tiền rõ ràng
- Giữ nguyên thông tin chính xác từ dữ liệu";

                userPrompt = $"Danh sách bất động sản:\n{propertyText}\n\nCâu hỏi: {request.Question}";
            }
            else
            {
                systemPrompt = "Bạn là một trợ lý thông minh. Hãy trả lời câu hỏi của người dùng một cách lịch sự và trung lập.";
                userPrompt = request.Question;
            }

            // 4. Gửi prompt chính để trả lời
            var messages = new[]
            {
        new { role = "system", content = systemPrompt },
        new { role = "user", content = userPrompt }
    };

            var finalPayload = new
            {
                model = "gpt-4o-mini",
                messages = messages,
                temperature = 0.7
            };

            var finalContent = new StringContent(JsonSerializer.Serialize(finalPayload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", finalContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return BadRequest(new { error = "GPT lỗi rồi!", detail = errorContent });
            }

            var result = await response.Content.ReadFromJsonAsync<OpenAiResponse>();
            var answer = result?.Choices?.FirstOrDefault()?.Message?.Content;

            return Ok(new { answer });
        }

        public class RealEstateCriteria
        {
            public int? MaxPrice { get; set; }
            public int? MinArea { get; set; }
            public int? Bedrooms { get; set; }
            public string? Location { get; set; }
        }

        public class ChatRequest
        {
            public string Question { get; set; }
        }

        public class OpenAiResponse
        {
            public List<Choice> Choices { get; set; }

            public class Choice
            {
                public Message Message { get; set; }
            }

            public class Message
            {
                public string Role { get; set; }
                public string Content { get; set; }
            }
        }
    }
}
