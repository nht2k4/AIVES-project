namespace AIVES.Business.Interfaces;

// Exchanges: các cặp (câu hỏi, câu trả lời) của câu chính hiện tại, câu cuối cùng là câu vừa được trả lời.
// AllowExternalAi = false: môn không cho gửi câu trả lời của sinh viên ra dịch vụ AI bên ngoài, chỉ dùng luật dự phòng.
public record FollowUpContext(
    string Question, string? ExpectedPoints, string Locale,
    IReadOnlyList<(string Question, string Answer)> Exchanges, bool AllowExternalAi = true);

public record FollowUpDecision(bool Ask, string? Question, string? Reason);

public interface IFollowUpGenerator
{
    // Không bao giờ ném lỗi: AI lỗi, chậm quá ct (thời gian chờ AI của môn) hoặc không được phép thì dùng luật dự phòng
    Task<FollowUpDecision> DecideAsync(FollowUpContext context, CancellationToken ct = default);
}
