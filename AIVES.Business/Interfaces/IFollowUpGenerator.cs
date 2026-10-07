namespace AIVES.Business.Interfaces;

// Exchanges: các cặp (câu hỏi, câu trả lời) của câu chính hiện tại, câu cuối cùng là câu vừa được trả lời
public record FollowUpContext(
    string Question, string? ExpectedPoints, string Locale,
    IReadOnlyList<(string Question, string Answer)> Exchanges);

public record FollowUpDecision(bool Ask, string? Question, string? Reason);

public interface IFollowUpGenerator
{
    Task<FollowUpDecision> DecideAsync(FollowUpContext context, CancellationToken ct = default);
}
