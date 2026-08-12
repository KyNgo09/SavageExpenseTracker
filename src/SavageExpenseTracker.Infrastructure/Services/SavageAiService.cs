using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SavageExpenseTracker.Application.Interfaces;
using SavageExpenseTracker.Infrastructure.Options;

namespace SavageExpenseTracker.Infrastructure.Services
{
    public class SavageAiService : ISavageAiService
    {
        private readonly HttpClient _httpClient;
        private readonly AiOptions _options;
        private readonly ILogger<SavageAiService> _logger;

        public SavageAiService(HttpClient httpClient, IOptionsMonitor<AiOptions> options, ILogger<SavageAiService> logger)
        {
            _httpClient = httpClient;
            _options = options.CurrentValue;
            _logger = logger;
        }

        public async Task<string> GenerateSavageCommentAsync(string? description, decimal amount, decimal timeWork, string categoryName)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey) || string.IsNullOrWhiteSpace(_options.ApiUrl))
            {
                _logger.LogWarning("AI options are not configured. Skipping AI comment generation.");
                return FallbackComment(timeWork);
            }

            try
            {
                var systemPrompt = @"Bạn là 'Bé Slime' - một trợ lý tài chính mỏ hỗn nhưng rất thông minh, biết điều và dễ thương. 
                                Bạn luôn gọi người dùng là 'chủ nhân'. 

                                TRỌNG TÂM CỐT LÕI: Đánh giá độ đắt/rẻ dựa trên TỶ LỆ CÔNG SỨC LAO ĐỘNG (timeWork) SO VỚI THỜI GIAN SỬ DỤNG/GIÁ TRỊ CỦA MÓN ĐỒ, không nhìn vào số tiền tuyệt đối hay con số giờ cố định!

                                [NGUYÊN TẮC ĐÁNH GIÁ GIÁ TRỊ TƯƠNG ĐỐI]

                                1. NHÓM ĐẦU TƯ & SINH TỒN (Sức khỏe, Học tập, Nhà cửa & Hóa đơn):
                                -> Hầu hết đều LÀ ĐẦU TƯ XỨNG ĐÁNG. Hãy KHEN NGỢI và ĐỘNG VIÊN chủ nhân. 
                                -> Chi tiền cho sức khỏe, tri thức hay sự an toàn luôn luôn đúng đắn!

                                2. NHÓM TIÊU DÙNG MỘT LẦN / NGẮN HẠN (Ăn uống, Di chuyển, Tiện ích hàng ngày):
                                -> So sánh timeWork với 'Niềm vui ngắn hạn':
                                    + Nếu timeWork nhỏ (chỉ chiếm một phần ngắn trong ngày làm việc): KHEN NGỢI nạp năng lượng/đi lại tốt.
                                    + Nếu timeWork chiếm TỶ LỆ LỚN thời gian cày cuốc trong ngày (chỉ để ăn 1 bữa đắt đỏ hoặc đi Grab ngắn), hoặc ĂN ĐÊM KHUYA: CÀ KHỊA xéo sắc vì đổi quá nhiều sức lao động lấy niềm vui chóng tàn.

                                3. NHÓM TỰ THƯỞNG & DÙNG LÂU DÀI (Mua sắm, Giải trí, Game, Mỹ phẩm, Đu đưa):
                                -> So sánh timeWork với 'Thời hạn sử dụng':
                                    + Món đồ dùng được nhiều tháng/năm mà timeWork hợp lý: KHEN NGỢI/Nhắc nhở vui vẻ.
                                    + Món đồ tiêu sản, game/quẩy tức thời mà tốn TỶ LỆ LỚN thời gian cày cuốc, HOẶC KHÔNG GHI MÔ TẢ (nghi vấn lén lút hoang phí): CÀ KHỊA MỎ HỖN! Mỉa mai số giờ cày cuốc bị phung phí.

                                NGUYÊN TẮC PHẢN HỒI:
                                - Đưa ra ĐÚNG 1 PHẢN HỒI (tối đa 2 câu, dưới 30 từ).
                                - Hài hước, phản ánh đúng giá trị thời gian lao động.";

                var timeLogged = DateTime.Now.ToString("HH:mm - dddd");

                var userPrompt = $"[DỮ LIỆU KHOẢN CHI]\n" +
                                $"- Danh mục: '{categoryName}'\n" +
                                $"- Mô tả: '{description ?? "Không có mô tả (Người dùng giấu)"}'\n" +
                                $"- Số tiền: {amount:N0} VNĐ\n" +
                                $"- Thời gian đổi bằng sức lao động: {timeWork:N1} giờ\n" +
                                $"- Thời điểm chi tiêu: {timeLogged}";

                var requestBody = new
                {
                    system_instruction = new
                    {
                        parts = new[] { new { text = systemPrompt } }
                    },
                    contents = new[]
                    {
                        new
                        {
                            parts = new[] { new { text = userPrompt } }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.8,
                        maxOutputTokens = 100
                    }
                };

                var url = $"{_options.ApiUrl.TrimEnd('/')}/{_options.Model}:generateContent?key={_options.ApiKey}";
                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(url, jsonContent);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Gemini API request failed with status {StatusCode}: {Reason}. Details: {ErrorBody}", response.StatusCode, response.ReasonPhrase, errorBody);
                    return FallbackComment(timeWork);
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);

                var commentText = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return string.IsNullOrWhiteSpace(commentText) ? FallbackComment(timeWork) : commentText.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI comment: {Message}", ex.Message);
                return FallbackComment(timeWork);
            }
        }

        private static string FallbackComment(decimal timeWork)
        {
            if (timeWork <= 0.5m)
            {
                string[] smallResponses = new[]
                {
                    $"Mất khoảng {timeWork:N1} giờ cày cuốc thôi, nạp năng lượng rồi chiến tiếp nha chủ nhân!",
                    $"Khoản này nhẹ nhàng ({timeWork:N1} giờ làm), Bé Slime duyệt cho chủ nhân đó nha!",
                    $"Tốn có {timeWork:N1} giờ lao động thôi, không đáng kể, cố gắng cày lại nha chủ nhân!"
                };
                return GetRandomResponse(smallResponses);
            }

            if (timeWork <= 3.0m)
            {
                string[] mediumResponses = new[]
                {
                    $"Bay màu {timeWork:N1} giờ cày cuốc bơ phờ rồi đó chủ nhân ơi, nhớ bớt bớt lại nha!",
                    $"Chủ nhân vừa đổi {timeWork:N1} giờ làm việc lấy khoản này, nhớ cân nhắc kỹ cho lần sau nhé!",
                    $"Bé Slime ghi nhận {timeWork:N1} giờ lao động đã ra đi, chi tiêu cẩn thận hơn nha chủ nhân!"
                };
                return GetRandomResponse(mediumResponses);
            }

            string[] heavyResponses = new[]
            {
                $"Bé Slime xin cạn lời! Chủ nhân vừa nướng sạch hơn {timeWork:N1} giờ cày cuốc vất vả vào khoản này đấy!",
                $"Trời ơi! {timeWork:N1} giờ lao động khổ sai đã 'bay màu' chỉ trong một nốt nhạc kìa chủ nhân!",
                $"Bé Slime xỉu đây! Mất hẳn {timeWork:N1} giờ cày cuốc bơ phờ cho khoản chi này rồi đó!"
            };
            return GetRandomResponse(heavyResponses);

            static string GetRandomResponse(string[] responses) 
                => responses[Random.Shared.Next(responses.Length)];
        }
    }
}