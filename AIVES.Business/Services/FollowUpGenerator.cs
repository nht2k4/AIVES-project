using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIVES.Business.Interfaces;
using AIVES.Business.Models;

namespace AIVES.Business.Services;

// TV6 - Ass3 - việc 6.2. "Giám khảo AI": quyết định có hỏi xoáy không.
// Xem AIVES.Tests/Business/AiAndVoiceTests.cs (FollowUpGeneratorFallbackTests) và AIVES_PhanCong_Ass2_Ass3.md.
public class FollowUpGenerator : IFollowUpGenerator
{
    private readonly AiOptions _options;
    private static readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(20) };

    public FollowUpGenerator(AiOptions options)
    {
        _options = options ?? new AiOptions();
    }

    public async Task<FollowUpDecision> DecideAsync(FollowUpContext context, CancellationToken ct = default)
    {
        // Nếu môn cấm dùng AI bên ngoài hoặc chưa cấu hình ApiKey -> luôn dùng luật dự phòng
        if (!context.AllowExternalAi || string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return DecideFallback(context);
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(20));

            var decision = await CallDeepSeekAsync(context, cts.Token);
            if (decision != null && !string.IsNullOrWhiteSpace(decision.Question))
            {
                return decision;
            }

            return DecideFallback(context);
        }
        catch
        {
            // Mọi lỗi mạng/hết quota/timeout -> rơi về luật dự phòng an toàn
            return DecideFallback(context);
        }
    }

    private async Task<FollowUpDecision?> CallDeepSeekAsync(FollowUpContext context, CancellationToken ct)
    {
        var isEnglish = context.Locale.StartsWith("en", StringComparison.OrdinalIgnoreCase);

        var systemPrompt = isEnglish
            ? "You are an oral exam interviewer. Analyze the student's answer based on the main question and expected key points. Decide whether to ask a follow-up question. Respond strictly with JSON format: {\"ask\": boolean, \"question\": string, \"reason\": string}."
            : "Bạn là giám khảo phỏng vấn thi vấn đáp. Hãy phân tích câu trả lời của thí sinh dựa vào câu hỏi chính và các ý chính mong đợi. Quyết định xem có cần hỏi xoáy (làm rõ thêm) không. Trả về đúng định dạng JSON: {\"ask\": boolean, \"question\": string, \"reason\": string}.";

        var exchangesText = string.Join("\n", context.Exchanges.Select((ex, idx) =>
            $"Lượt {idx + 1}:\n- Câu hỏi: {ex.Question}\n- Trả lời: {ex.Answer}"));

        var userPrompt = $"Câu hỏi chính: {context.Question}\n" +
                         $"Ý chính mong đợi: {context.ExpectedPoints ?? "(Không có)"}\n" +
                         $"Ngôn ngữ: {context.Locale}\n" +
                         $"Lịch sử trao đổi:\n{exchangesText}\n\n" +
                         "Hãy trả về JSON có cấu trúc {\"ask\": boolean, \"question\": string, \"reason\": string}.";

        var requestBody = new
        {
            model = string.IsNullOrWhiteSpace(_options.Model) ? "deepseek-flash" : _options.Model,
            max_tokens = 300,
            // deepseek-flash mặc định bật "thinking": tiêu token vào suy luận và có thể trả nội dung rỗng.
            // Tắt đi thì trả lời ngay, ít token nhất (rẻ nhất) và nhanh nhất cho nhịp vấn đáp.
            thinking = new { type = "disabled" },
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            response_format = new { type = "json_object" }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.deepseek.com/chat/completions")
        {
            Content = JsonContent.Create(requestBody)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadFromJsonAsync<DeepSeekChatResponse>(cancellationToken: ct);
        var content = json?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var parsed = JsonSerializer.Deserialize<DeepSeekFollowUpPayload>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (parsed == null)
        {
            return null;
        }

        return new FollowUpDecision(parsed.Ask, parsed.Question, parsed.Reason);
    }

    private static FollowUpDecision DecideFallback(FollowUpContext context)
    {
        var isEnglish = context.Locale.StartsWith("en", StringComparison.OrdinalIgnoreCase);
        var lastAnswer = context.Exchanges.LastOrDefault().Answer?.Trim() ?? string.Empty;

        // Đếm số từ của câu trả lời cuối cùng
        var words = lastAnswer.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var wordCount = words.Length;

        // Luật 1: Câu trả lời cuối dưới 12 từ -> hỏi giải thích rõ hơn
        if (wordCount < 12)
        {
            var question = isEnglish
                ? "Could you explain this point more clearly?"
                : "Bạn có thể giải thích rõ hơn ý này được không?";
            var reason = isEnglish
                ? "Answer is too brief."
                : "Câu trả lời quá ngắn.";

            return new FollowUpDecision(true, question, reason);
        }

        // Luật 2: Nếu không có ý chính mong đợi -> chấp nhận câu trả lời dài
        if (string.IsNullOrWhiteSpace(context.ExpectedPoints))
        {
            return new FollowUpDecision(false, null, null);
        }

        var points = context.ExpectedPoints
            .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        if (points.Count == 0)
        {
            return new FollowUpDecision(false, null, null);
        }

        // Toàn bộ câu trả lời của thí sinh từ trước đến nay (chuẩn hóa không dấu, viết thường)
        var allAnswersCombined = string.Join(" ", context.Exchanges.Select(e => e.Answer ?? ""));
        var normalizedAnswers = NormalizeText(allAnswersCombined);

        // Các câu hỏi trước đó trong phiên này (bỏ lượt cuối vừa hỏi)
        var earlierQuestions = context.Exchanges
            .Take(context.Exchanges.Count - 1)
            .Select(e => NormalizeText(e.Question))
            .ToList();

        // Tìm ý chính đầu tiên chưa nhắc tới và chưa từng bị hỏi lại
        foreach (var point in points)
        {
            var normalizedPoint = NormalizeText(point);

            // Bỏ qua nếu ý này đã từng được hỏi trong các câu hỏi trước
            var alreadyAsked = earlierQuestions.Any(q => q.Contains(normalizedPoint));
            if (alreadyAsked)
            {
                continue;
            }

            // Kiểm tra xem thí sinh đã nhắc đến ý này chưa
            if (!normalizedAnswers.Contains(normalizedPoint))
            {
                var question = isEnglish
                    ? $"You haven't mentioned \"{point}\". Could you elaborate on that?"
                    : $"Bạn chưa nhắc đến \"{point}\". Bạn làm rõ được không?";
                var reason = isEnglish
                    ? $"Missing key point: \"{point}\"."
                    : $"Chưa nhắc đến ý chính: \"{point}\".";

                return new FollowUpDecision(true, question, reason);
            }
        }

        // Đủ ý hoặc các ý thiếu đều đã hỏi lại rồi -> không hỏi thêm
        return new FollowUpDecision(false, null, null);
    }

    private static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        var result = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        // Xử lý ký tự đ/Đ tiếng Việt
        return result.Replace('đ', 'd');
    }

    private class DeepSeekChatResponse
    {
        public List<DeepSeekChoice>? Choices { get; set; }
    }

    private class DeepSeekChoice
    {
        public DeepSeekMessage? Message { get; set; }
    }

    private class DeepSeekMessage
    {
        public string? Content { get; set; }
    }

    private class DeepSeekFollowUpPayload
    {
        public bool Ask { get; set; }
        public string? Question { get; set; }
        public string? Reason { get; set; }
    }
}
